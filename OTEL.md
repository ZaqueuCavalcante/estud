# OpenTelemetry no Estud: como está hoje e o que falta pro Grafana Cloud

> Conferido contra o código e contra a documentação do Grafana Cloud em 2026-09-11.

**Resumo:** o código já produz traces, métricas e logs via OTLP em produção (`OpenTelemetry:Enabled: true` em `Back/appsettings.Production.json:46`). Nada no repositório diz para onde enviar. Os exporters caem no padrão `localhost:4317` via gRPC, e o gateway OTLP do Grafana Cloud só documenta HTTP. Então hoje os dados ou são descartados ou vão para onde as variáveis do Railway apontarem, que não ficam no repositório.

## Como está hoje

| Sinal | Quem coleta | Como exporta | Onde está |
|---|---|---|---|
| **Traces** | ASP.NET Core, HttpClient, Npgsql e 3 fontes próprias (commands, domain events, webhook events) | `AddOtlpExporter()` sem opções | `Back/Configs/OpenTelemetryConfigs.cs:37-48` |
| **Métricas** | ASP.NET Core, Kestrel, HttpClient, Npgsql e runtime | `AddOtlpExporter()` sem opções | `Back/Configs/OpenTelemetryConfigs.cs:24-36` |
| **Logs** | Serilog, que roda à parte do SDK do OTel | `Serilog.Sinks.OpenTelemetry` | `Back/Configs/HostConfigs.cs:13` |

### O papel do Serilog

O Serilog é o único caminho de log do backend:

- **Substitui o logging padrão do .NET.** O `UseSerilog` (`Back/Configs/HostConfigs.cs:7`) troca o provider do `Microsoft.Extensions.Logging`. Três tipos de log passam por ele:
  - o `ILogger<T>` injetado nos serviços (`DnsManager`, `EmailsService`)
  - os logs do framework (ASP.NET Core, EF Core)
  - o `Serilog.ILogger` injetado direto no `ExceptionsMiddleware`
- **O SDK do OTel não cuida de logs.** O `OpenTelemetryConfigs` só chama `WithMetrics` e `WithTracing`, sem `WithLogging`. Um log só chega no OTLP pelo sink do Serilog. São dois exporters independentes, cada um com sua configuração, e daí vêm os cuidados com variáveis de ambiente e `service.name` descritos abaixo.
- **Uma linha por request.** O `UseSerilogRequestLogging()` (`Back/Configs/HttpConfigs.cs:71`) entra no pipeline antes do `UseExceptions()` (`Back/Program.cs:35` e `:40`).

| Ambiente | Destinos | Nível mínimo |
|---|---|---|
| Development | Console | Information |
| Production | Console + OTLP | Information |
| Testing | Arquivo `estud_tests_logs.txt` (OTel desligado) | Error |

### O que já funciona bem

- **Propagação do trace pros jobs:** `AddCommand` guarda o `Activity.Current.Id` (`Back/Database/EstudDbContext.Commands.cs:32` e `:63`), e o `SaveDomainEventsInterceptor` faz o mesmo com os domain events. O `CommandsProcessor` e o `DomainEventsProcessor` retomam o trace com esse pai, e o retry de um command herda o mesmo pai. No Tempo, o request e o que ele disparou de forma assíncrona aparecem como um trace só.
- **Erro nos commands e domain events:** as falhas marcam status de erro e `AddException` no span (`Back/Commands/CommandsProcessor.cs:61-62`, `Back/DomainEvents/DomainEventsProcessor.cs:61-62`).
- **Chamadas de webhook de saída:** o `CallWebhookCommandHandler` roda dentro do span do command, e o `PostAsync` gera um span filho pela instrumentação de HttpClient.
- **Logs ligados aos traces:** o Serilog anexa TraceId/SpanId em cada log, e o sink manda os dois no OTLP. No Grafana dá pra ir do log pro trace.
- **Nome dos spans SQL:** o Npgsql já tem um nome de span customizado (`Back/Configs/EntityFrameworkConfigs.cs:13`).

## O que falta pra chegar no Grafana Cloud

### Direto no gateway ou via Alloy

O Grafana recomenda o **Grafana Alloy**, a distribuição deles do OpenTelemetry Collector, como arquitetura de produção. Mandar direto do SDK pro gateway OTLP aparece como opção de desenvolvimento/teste, ou pra quando não dá pra rodar um collector. Na doc, o envio direto tem duas desvantagens:

- confiabilidade limitada quando o transporte falha
- nenhum jeito de enriquecer a telemetria no meio do caminho (atributos, sampling, filtro)

No Railway, o Alloy seria um serviço a mais. **Recomendação:** começar direto no gateway, porque o volume do Estud é baixo e não exige infra nova. Passar pro Alloy se aparecer perda de dados ou se for preciso filtrar ou fazer tail sampling antes de gastar cota.

### Variáveis no Railway

Não precisa mexer no código, só configurar as variáveis no serviço do back. Os dois exporters leem as mesmas variáveis. O sink do Serilog 4.2.0, que ainda é a versão estável mais recente, lê `OTEL_EXPORTER_OTLP_ENDPOINT`, `_HEADERS`, `_PROTOCOL`, `OTEL_RESOURCE_ATTRIBUTES` e `OTEL_SERVICE_NAME`, e acrescenta `/v1/logs` sozinho (conferido no DLL do pacote).

```
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp-gateway-<REGION>.grafana.net/otlp
OTEL_EXPORTER_OTLP_HEADERS=Authorization=Basic <base64(instanceId:token)>
OTEL_RESOURCE_ATTRIBUTES=deployment.environment.name=production,service.namespace=estud
```

- **Onde pegar os valores:** no Grafana Cloud Portal, abrir o stack e clicar em **Configure** no tile **OpenTelemetry**. A tela gera o token e os valores exatos, inclusive a região.
- **Protocolo:** a doc só documenta `http/protobuf`. Sem essa variável, os dois exporters usam gRPC, que é o padrão tanto do exporter do .NET quanto do sink do Serilog.
- **Header:** usar espaço literal depois de `Basic`. A doc do Grafana só pede `%20` pra Python. Com espaço literal funciona nas duas libs do .NET.
- **Env var, não appsettings:** o sink do Serilog lê só variáveis de ambiente, não o `IConfiguration`. Se o endpoint for colocado no `appsettings.Production.json`, traces e métricas chegam no Grafana e os logs continuam indo pra `localhost`.
- **Cuidado com `OTEL_SERVICE_NAME`:** o `AddService("Back")` do código vence essa variável nos traces e métricas, mas no Serilog a variável vence. Se ela for definida, os logs ficam com um nome de serviço e os traces com outro, e a correlação quebra. O melhor é deixar sem essa variável ou trocar o código pra ler dela. O nome `Back` também é genérico demais pra um painel.

### Atributos de recurso

O que o Application Observability usa:

| Atributo | Status | Sem ele |
|---|---|---|
| `service.name` | Obrigatório | O serviço não é identificado |
| `deployment.environment.name` | Recomendado | Os serviços ficam sem ambiente ou no ambiente errado, e os baselines não funcionam. O nome antigo `deployment.environment` ainda é aceito. |
| `service.namespace` | Recomendado | Não dá pra filtrar nem agrupar por namespace |
| `service.version` | Recomendado | Não dá pra ver se uma versão nova introduziu um bug |
| `service.instance.id` | Recomendado | Não aparece o label de instância |

- Não usar `/` em `service.name` nem em `service.namespace`.
- O `AddService` do SDK gera um `service.instance.id` sozinho, mas o sink do Serilog não. Os logs saem com um resource diferente do dos traces. A correlação pelo TraceId continua funcionando, mas filtrar logs por instância não.

### Alternativa: logs pelo SDK do OTel

Os cuidados das duas seções anteriores existem porque os logs saem por um exporter separado. Existem dois jeitos de unificar:

1. Ligar `WithLogging()` no SDK e trocar o sink do Serilog por `UseSerilog(..., writeToProviders: true)`. O Serilog continua escrevendo no console e repassa os logs pro provider do OTel. Os três sinais passam a usar o mesmo exporter, a mesma configuração (inclusive `appsettings`) e o mesmo resource.
2. Usar a distribuição do Grafana (`Grafana.OpenTelemetry`). O Grafana a recomenda como caminho padrão pro Application Observability, e ela já suporta .NET 10. Ela cobre logs via `builder.Logging.AddOpenTelemetry(o => o.UseGrafana())`, mas não fala de Serilog: é a mesma troca do item 1, com instrumentações pré-empacotadas.

Nenhuma das duas é necessária pra ligar o Grafana, porque as variáveis de ambiente resolvem. Elas valem se a divergência entre os dois exporters incomodar.

## O que vai aparecer quando ligar (por impacto)

1. **Exceções não chegam em lugar nenhum.**
   - `Back/Middlewares/ExceptionsMiddleware.cs:28` loga só `message`, e em produção isso é o texto genérico "Erro ao executar essa ação.". O log sai sem stack trace e sem o objeto da exceção.
   - O `UseSerilogRequestLogging` vê o 500 e grava uma linha em nível Error, mas também sem a exceção.
   - O span do request fica com status de erro por causa do 500, mas sem o evento da exceção.

   É o maior buraco de observabilidade.
2. **Volume de logs alto.** `MinimumLevel: Information` não tem nenhum `Override`. O EF Core loga cada SQL executado em Information, e o ASP.NET Core loga início e fim de request, o que duplica o `UseSerilogRequestLogging`. O plano gratuito dá 50 GB de logs por mês, com 14 dias de retenção. Um override pra `Warning` em `Microsoft.EntityFrameworkCore` e `Microsoft.AspNetCore` resolve.
3. **Spans soltos no Tempo.**
   - Os processors de commands e de domain events rodam a cada 60 s (`Back/appsettings.Production.json:10-11`). A query de polling de cada um roda fora de qualquer span (`Back/Commands/CommandsProcessor.cs:27`), e o Npgsql cria um trace raiz pra cada execução. São pelo menos 2 traces por minuto (uns 86 mil por mês), sem contar os disparos do `BackgroundProcessorsTriggerMiddleware`.
   - Os requests de `/docs` e `openapi` também entram, porque o `AddAspNetCoreInstrumentation()` não tem `Filter`.
4. **Processor de webhooks recebidos desligado.**
   - A fonte `WebhookEventsProcessing` está no `AddSource` (`Back/Configs/OpenTelemetryConfigs.cs:43`), e o `ReceivedWebhookEventsProcessor` cria spans com ela.
   - Só que o job não é agendado no `QuartzConfigs`. A chave `WebhookEventsPollingIntervalInSeconds` existe no appsettings, mas ninguém lê. E nada no código grava em `received_webhook_events`. Hoje essa fonte não emite nenhum span.
   - Quando o job for ligado, o span vai começar sem pai (`Back/Webhooks/ReceivedWebhookEventsProcessor.cs:31`), porque a entidade não guarda `ActivityId`. O processamento não vai se ligar ao request que recebeu o webhook.
5. **Sampler:** `TraceIdRatioBasedSampler` sem `ParentBased` (`Back/Configs/OpenTelemetryConfigs.cs:46`). Com o ratio em 1.0 não faz diferença. Se baixar, ele passa a ignorar a decisão de amostragem de quem chamou.
6. **Nenhuma métrica de negócio.** Não existe nenhum `Meter` próprio. Commands processados, falhas, duração e atraso da fila seriam as métricas mais úteis pra alertas. O plano gratuito aceita 10 mil séries ativas.
7. **`WithMachineName` não faz nada.** Ele está no `Enrich` de todos os appsettings, mas o pacote `Serilog.Enrichers.Environment` não está no `Back.csproj`. O Serilog ignora o enricher sem avisar, e nenhum log sai com o nome da máquina.

## Limpeza (`Back/Back.csproj`)

- **Pacotes não usados:**
  - `OpenTelemetry.Instrumentation.Quartz` não está ligado.
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` também não, e ligar esse duplicaria os spans do Npgsql.
  - Os pacotes do Sentry e o `Serilog.Sinks.Seq` já saíram do csproj.
- **Versões desalinhadas** (resolvidas no `project.assets.json`):
  - OTel core e exporter estão em 1.15.3; Hosting e as instrumentações em 1.12.0. No NuGet, core, Hosting e exporter já estão em 1.18.0.
  - `Npgsql.OpenTelemetry` 9.0.3 roda sobre Npgsql 10.0.2. Já existe `Npgsql.OpenTelemetry` 10.0.3.
  - Funciona pela unificação do NuGet, mas vale alinhar.
- **`AddMeter("Microsoft.AspNetCore.Hosting")` é redundante** (`Back/Configs/OpenTelemetryConfigs.cs:33`) com `AddAspNetCoreInstrumentation()` no .NET 8+. Não faz mal nenhum.
- **A página `Web/app/pages/dev/overview.vue` está desatualizada:**
  - As linhas 267, 268, 276, 309 e 376 dizem que os logs não passam pelo OTLP e que o sink do Serilog não está configurado. Ele está (`Back/Configs/HostConfigs.cs:13`).
  - As linhas 80, 268 e 295 descrevem o OTLP como "traces e métricas", sem logs.
  - A linha 311 diz que os pacotes do Sentry estão no csproj.
  - As linhas 295 e 308 também dizem que não há destino definido. Essa parte continua certa até as variáveis do Railway serem configuradas.

## Fora do backend

- **Frontend sem telemetria:** o Nuxt não tem nada, e o trace não passa do front pra API. No Grafana Cloud, o caminho é o **Faro Web SDK** (Frontend Observability) com o pacote de tracing, que usa o OpenTelemetry-JS por baixo. O plano gratuito dá 50 mil sessões por mês. Se o front chamar a API em outra origem, as URLs precisam entrar no `propagateTraceHeaderCorsUrls`. O CORS da API já tem `AllowAnyHeader()` (`Back/Configs/CorsConfigs.cs:10`), então o `traceparent` passa.
- **Testar localmente antes de ligar em produção:** rodar `docker run -p 3000:3000 -p 4317:4317 -p 4318:4318 grafana/otel-lgtm` e pôr `Enabled: true` no Development. A imagem é só pra desenvolvimento, demo e teste.
  - Traces e métricas usam o `OTEL_EXPORTER_OTLP_ENDPOINT` que já está no `Back/appsettings.Development.json:55`.
  - Os logs caem no padrão `localhost:4317` via gRPC do sink.
  - Os dois batem com a imagem.

## Próximos passos sugeridos

1. Corrigir o log de exceções no `ExceptionsMiddleware` (item 1).
2. Configurar as variáveis `OTEL_*` no Railway, com `deployment.environment.name` e `service.namespace`.
3. Adicionar os overrides de nível de log do Serilog (item 2).
4. Filtrar os spans de polling e de `/docs` (item 3), antes que eles ocupem o Tempo.

## Fontes

- [Send data using OpenTelemetry Protocol (OTLP)](https://grafana.com/docs/grafana-cloud/send-data/otlp/send-data-otlp/)
- [Application Observability resource attributes](https://grafana.com/docs/grafana-cloud/monitor-applications/application-observability/setup/resource-attributes/)
- [Instrument a .NET application](https://grafana.com/docs/opentelemetry/instrument/grafana-dotnet/) e [grafana-opentelemetry-dotnet](https://github.com/grafana/grafana-opentelemetry-dotnet)
- [Grafana Cloud pricing](https://grafana.com/pricing/)
- [Frontend Observability / Faro](https://grafana.com/docs/grafana-cloud/monitor-applications/frontend-observability/instrument/faro/)
- [docker-otel-lgtm](https://github.com/grafana/docker-otel-lgtm)
