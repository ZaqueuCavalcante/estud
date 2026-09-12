# OpenTelemetry no Estud: como está hoje e o que falta

> Conferido contra o código em 2026-09-11.

**Resumo:** em produção, traces, métricas e logs vão direto pro gateway OTLP do Grafana Cloud, sem collector no meio. O endpoint e o token ficam nas variáveis `OTEL_*` do serviço do back na Railway, fora do repositório. O código só liga os exporters quando `OpenTelemetry:Enabled` é `true` (`Back/appsettings.Production.json:51`).

## Como está hoje

| Sinal | Quem coleta | Como exporta | Onde está |
|---|---|---|---|
| **Traces** | ASP.NET Core, HttpClient, Npgsql e 3 fontes próprias (commands, domain events, webhook events) | `AddOtlpExporter()` sem opções | `Back/Configs/OpenTelemetryConfigs.cs:37-48` |
| **Métricas** | ASP.NET Core, Kestrel, HttpClient, Npgsql e runtime | `AddOtlpExporter()` sem opções | `Back/Configs/OpenTelemetryConfigs.cs:24-36` |
| **Logs** | Serilog, que roda à parte do SDK do OTel | `Serilog.Sinks.OpenTelemetry` | `Back/Configs/HostConfigs.cs:13` |

### O papel do Serilog

O Serilog é o único caminho de log do backend:

- **Substitui o logging padrão do .NET.** O `UseSerilog` (`Back/Configs/HostConfigs.cs:7`) troca o provider do `Microsoft.Extensions.Logging`. Tudo que usa `ILogger<T>` passa por ele: serviços, middlewares e os logs do framework (ASP.NET Core, EF Core).
- **O SDK do OTel não cuida de logs.** O `OpenTelemetryConfigs` só chama `WithMetrics` e `WithTracing`, sem `WithLogging`. Um log só chega no OTLP pelo sink do Serilog. São dois exporters independentes, cada um com sua configuração, e daí vêm os cuidados listados abaixo.
- **Uma linha por request.** O `UseSerilogRequestLogging()` (`Back/Configs/HttpConfigs.cs:71`) entra no pipeline antes do `UseExceptions()` (`Back/Program.cs:35` e `:40`).

| Ambiente | Destinos | Nível mínimo |
|---|---|---|
| Development | Console | Information; `Microsoft` e `System` em Warning, SQL do EF em Information |
| Production | Console + Grafana Cloud | Information; `Microsoft` e `System` em Warning |
| Testing | Arquivo `estud_tests_logs.txt` (OTel desligado) | Error |

### O que já funciona bem

- **Propagação do trace pros jobs:** `AddCommand` guarda o `Activity.Current.Id` (`Back/Database/EstudDbContext.Commands.cs:32` e `:63`), e o `SaveDomainEventsInterceptor` faz o mesmo com os domain events. O `CommandsProcessor` e o `DomainEventsProcessor` retomam o trace com esse pai, e o retry de um command herda o mesmo pai. No Tempo, o request e o que ele disparou de forma assíncrona aparecem como um trace só.
- **Erro nos commands e domain events:** as falhas marcam status de erro e `AddException` no span (`Back/Commands/CommandsProcessor.cs:61-62`, `Back/DomainEvents/DomainEventsProcessor.cs:61-62`).
- **Exceções dos requests:** o `ExceptionsMiddleware` loga a exceção inteira, com stack trace, e também a registra no span do request com status de erro e `AddException` (`Back/Middlewares/ExceptionsMiddleware.cs:28-31`). No Tempo, o erro aparece com a exceção.
- **Chamadas de webhook de saída:** o `CallWebhookCommandHandler` roda dentro do span do command, e o `PostAsync` gera um span filho pela instrumentação de HttpClient.
- **Logs ligados aos traces:** o Serilog anexa TraceId/SpanId em cada log, e o sink manda os dois no OTLP. No Grafana dá pra ir do log pro trace.
- **Nome dos spans SQL:** o Npgsql já tem um nome de span customizado (`Back/Configs/EntityFrameworkConfigs.cs:14`).
- **Query fora de trace não vira span:** o `ConfigureCommandFilter` e o `ConfigureBatchFilter` (`Back/Configs/EntityFrameworkConfigs.cs:16-17`) descartam queries sem span ativo, como o polling dos processors a cada 60 s. As métricas do Npgsql continuam medindo a duração de todas as queries.

## Cuidados ao mexer na configuração

- **Env var, não appsettings:** o sink do Serilog lê só variáveis de ambiente, não o `IConfiguration`. Se o endpoint for pro `appsettings.Production.json`, traces e métricas continuam chegando e os logs vão pra `localhost`.
- **Não definir `OTEL_SERVICE_NAME`:** o `AddService("Back")` do código vence essa variável nos traces e métricas, mas no Serilog a variável vence. Se ela for definida, os logs ficam com um nome de serviço e os traces com outro, e a correlação quebra. O mesmo vale pra `service.instance.id` dentro de `OTEL_RESOURCE_ATTRIBUTES`.
- **Não trocar o `service.namespace` nem o nome do serviço sem planejar:** os dois formam o label `job` das métricas. Trocar qualquer um separa as séries antigas das novas e quebra dashboards e alertas que filtram pelo valor antigo.
- **Protocolo e header:** o gateway só aceita `http/protobuf`. No header, usar espaço literal depois de `Basic`, sem aspas. Vale lembrar disso ao trocar o token.

## Pendências (por impacto)

1. **Requests de `/docs` e `openapi` no Tempo.** O `AddAspNetCoreInstrumentation()` (`Back/Configs/OpenTelemetryConfigs.cs:42`) não tem `Filter`, então cada acesso à documentação vira um trace. Um `Filter` que ignore esses caminhos resolve. O filtro roda antes do `UsePathBase`, então o caminho ainda chega com o prefixo `/api`.
2. **Processor de webhooks recebidos desligado.**
   - A fonte `WebhookEventsProcessing` está no `AddSource` (`Back/Configs/OpenTelemetryConfigs.cs:43`), e o `ReceivedWebhookEventsProcessor` cria spans com ela.
   - Só que o job não é agendado no `QuartzConfigs`. A chave `WebhookEventsPollingIntervalInSeconds` existe no appsettings, mas ninguém lê. E nada no código grava em `received_webhook_events`. Hoje essa fonte não emite nenhum span.
   - Quando o job for ligado, o span vai começar sem pai (`Back/Webhooks/ReceivedWebhookEventsProcessor.cs:31`), porque a entidade não guarda `ActivityId`. O processamento não vai se ligar ao request que recebeu o webhook.
3. **Sampler:** `TraceIdRatioBasedSampler` sem `ParentBased` (`Back/Configs/OpenTelemetryConfigs.cs:46`). Com o ratio em 1.0 não faz diferença. Se baixar, ele passa a ignorar a decisão de amostragem de quem chamou.
4. **Nenhuma métrica de negócio.** Não existe nenhum `Meter` próprio. Commands processados, falhas, duração e atraso da fila seriam as métricas mais úteis pra alertas. O plano gratuito aceita 10 mil séries ativas.
5. **Resource dos logs diferente do dos traces.** O `AddService` do SDK gera um `service.instance.id` sozinho, mas o sink do Serilog não. A correlação pelo TraceId funciona, mas filtrar logs por instância não. Resolve junto com a unificação descrita abaixo.

### Opcional: logs pelo SDK do OTel

Os cuidados com `OTEL_SERVICE_NAME` e o item 5 existem porque os logs saem por um exporter separado. Existem dois jeitos de unificar:

1. Ligar `WithLogging()` no SDK e trocar o sink do Serilog por `UseSerilog(..., writeToProviders: true)`. O Serilog continua escrevendo no console e repassa os logs pro provider do OTel. Os três sinais passam a usar o mesmo exporter, a mesma configuração (inclusive `appsettings`) e o mesmo resource.
2. Usar a distribuição do Grafana (`Grafana.OpenTelemetry`). O Grafana a recomenda como caminho padrão pro Application Observability, e ela já suporta .NET 10. Ela cobre logs via `builder.Logging.AddOpenTelemetry(o => o.UseGrafana())`, mas não fala de Serilog: é a mesma troca do item 1, com instrumentações pré-empacotadas.

### Opcional: Grafana Alloy

O envio direto pro gateway é o que o Grafana chama de arquitetura de desenvolvimento/teste: confiabilidade limitada quando o transporte falha, e nenhum jeito de filtrar ou amostrar no meio do caminho. Vale colocar o Alloy como serviço na Railway se aparecer perda de dados, ou se for preciso fazer tail sampling ou filtrar spans antes de gastar cota.

## Limpeza

- **Pacotes não usados** (`Back/Back.csproj`):
  - `OpenTelemetry.Instrumentation.Quartz` não está ligado.
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` também não, e ligar esse duplicaria os spans do Npgsql.
- **Versões desalinhadas** (resolvidas no `project.assets.json`):
  - OTel core e exporter estão em 1.15.3; Hosting e as instrumentações em 1.12.0. No NuGet, core, Hosting e exporter já estão em 1.18.0.
  - `Npgsql.OpenTelemetry` 9.0.3 roda sobre Npgsql 10.0.2. Já existe `Npgsql.OpenTelemetry` 10.0.3.
  - Funciona pela unificação do NuGet, mas vale alinhar.
- **`AddMeter("Microsoft.AspNetCore.Hosting")` é redundante** (`Back/Configs/OpenTelemetryConfigs.cs:33`) com `AddAspNetCoreInstrumentation()` no .NET 8+. Não faz mal nenhum.
- **`WithMachineName` no Development** (`Back/appsettings.Development.json:48`): o pacote `Serilog.Enrichers.Environment` não está no csproj, e o Serilog ignora o enricher sem avisar. Já saiu dos outros appsettings.
- **A página `Web/app/pages/dev/overview.vue` está desatualizada:**
  - As linhas 267, 268, 276, 309 e 376 dizem que os logs não passam pelo OTLP e que o sink do Serilog não está configurado.
  - As linhas 80, 268 e 295 descrevem o OTLP como "traces e métricas", sem logs.
  - As linhas 295 e 308 dizem que não há destino definido em produção. Hoje é o Grafana Cloud.
  - A linha 311 diz que os pacotes do Sentry estão no csproj.

## Fora do backend

- **Frontend sem telemetria:** o Nuxt não tem nada, e o trace não passa do front pra API. No Grafana Cloud, o caminho é o **Faro Web SDK** (Frontend Observability) com o pacote de tracing, que usa o OpenTelemetry-JS por baixo. O plano gratuito dá 50 mil sessões por mês. Se o front chamar a API em outra origem, as URLs precisam entrar no `propagateTraceHeaderCorsUrls`. O CORS da API já tem `AllowAnyHeader()` (`Back/Configs/CorsConfigs.cs:10`), então o `traceparent` passa.
- **Testar localmente:** rodar `docker run -p 3000:3000 -p 4317:4317 -p 4318:4318 grafana/otel-lgtm` e pôr `Enabled: true` no Development. A imagem é só pra desenvolvimento, demo e teste.
  - Traces e métricas usam o `OTEL_EXPORTER_OTLP_ENDPOINT` que já está no `Back/appsettings.Development.json:61`.
  - Os logs caem no padrão `localhost:4317` via gRPC do sink.
  - Os dois batem com a imagem.

## Próximos passos sugeridos

1. Filtrar os requests de `/docs` e `openapi` (item 1).

## Fontes

- [Send data using OpenTelemetry Protocol (OTLP)](https://grafana.com/docs/grafana-cloud/send-data/otlp/send-data-otlp/)
- [Instrument a .NET application](https://grafana.com/docs/opentelemetry/instrument/grafana-dotnet/) e [grafana-opentelemetry-dotnet](https://github.com/grafana/grafana-opentelemetry-dotnet)
- [Grafana Cloud pricing](https://grafana.com/pricing/)
- [Frontend Observability / Faro](https://grafana.com/docs/grafana-cloud/monitor-applications/frontend-observability/instrument/faro/)
- [docker-otel-lgtm](https://github.com/grafana/docker-otel-lgtm)
