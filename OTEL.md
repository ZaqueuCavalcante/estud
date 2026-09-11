# OpenTelemetry no Estud: como está hoje e o que falta pro Grafana Cloud

**Resumo:** o código já produz traces, métricas e logs via OTLP em produção (`OpenTelemetry:Enabled: true` em `Back/appsettings.Production.json:46`). Nada no repositório diz para onde enviar. Os exporters caem no padrão `localhost:4317` via gRPC, e o Grafana Cloud só aceita HTTP. Então hoje os dados ou são descartados ou vão para onde as variáveis do Railway apontarem, que não ficam no repositório.

## Como está hoje

| Sinal | Quem coleta | Como exporta | Onde está |
|---|---|---|---|
| **Traces** | ASP.NET Core, HttpClient, Npgsql e 3 fontes próprias (commands, domain events, webhook events) | `AddOtlpExporter()` sem opções | `Back/Configs/OpenTelemetryConfigs.cs:39-50` |
| **Métricas** | ASP.NET Core, Kestrel, HttpClient, Npgsql e runtime | `AddOtlpExporter()` sem opções | `Back/Configs/OpenTelemetryConfigs.cs:26-38` |
| **Logs** | Serilog, que roda à parte do SDK do OTel | `Serilog.Sinks.OpenTelemetry` | `Back/Configs/HostConfigs.cs:13` |

Pontos bons que já existem:

- **Propagação do trace pros jobs:** `AddCommand` guarda o `Activity.Current.Id` e o `CommandsProcessor`/`DomainEventsProcessor` retomam o trace com esse pai. No Tempo, o request e o command assíncrono que ele disparou aparecem como um trace só.
- **Erro nos commands:** as falhas marcam status de erro e `AddException` no span.
- **Logs ligados aos traces:** o Serilog anexa TraceId/SpanId em cada log, então no Grafana dá pra ir do log pro trace.
- **Nome dos spans SQL:** o Npgsql já tem um nome de span customizado (`Back/Configs/EntityFrameworkConfigs.cs:13`).

## O que falta pra chegar no Grafana Cloud

Não precisa mexer no código, só configurar variáveis no serviço do back no Railway. Os dois exporters leem as mesmas variáveis. O sink do Serilog 4.2.0 lê `OTEL_EXPORTER_OTLP_ENDPOINT`, `_HEADERS`, `_PROTOCOL`, `OTEL_RESOURCE_ATTRIBUTES` e `OTEL_SERVICE_NAME`, e acrescenta `/v1/logs` sozinho (conferido no DLL do pacote).

```
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp-gateway-prod-<região>.grafana.net/otlp
OTEL_EXPORTER_OTLP_HEADERS=Authorization=Basic <base64(instanceId:token)>
OTEL_RESOURCE_ATTRIBUTES=deployment.environment.name=production,service.namespace=estud
```

- **Onde pegar os valores:** o tile *Connections → OpenTelemetry (OTLP)* do Grafana Cloud gera os valores exatos.
- **Protocolo:** o `http/protobuf` é obrigatório. Sem ele, os dois exporters usam gRPC e o gateway do Grafana recusa.
- **Header:** prefira espaço literal em vez de `%20` depois de `Basic`. O espaço funciona nas duas libs. O `%20` depende de cada uma decodificar.
- **Env var, não appsettings:** o sink do Serilog lê só variáveis de ambiente, não o `IConfiguration`. Se o endpoint for colocado no `appsettings.Production.json`, traces e métricas chegam no Grafana e os logs continuam indo pra `localhost`.
- **Cuidado com `OTEL_SERVICE_NAME`:** o `AddService("Back")` do código vence essa variável nos traces e métricas, mas no Serilog a variável vence. Se ela for definida, os logs ficam com um nome de serviço e os traces com outro, e a correlação no Grafana quebra. Deixar sem essa variável ou trocar o código pra ler dela. O nome `Back` também é genérico demais pra um painel.

## O que vai aparecer quando ligar (por impacto)

1. **Exceções não chegam em lugar nenhum.** `Back/Middlewares/ExceptionsMiddleware.cs:28` loga só `message`, e em produção isso é o texto genérico "Erro ao executar essa ação.". Sem stack trace e sem o objeto da exceção no log. Como a exceção é engolida antes de subir, o span do request também fica sem o evento de exceção. É o maior buraco de observabilidade.
2. **Volume de logs alto.** `MinimumLevel: Information` não tem nenhum `Override`. O EF Core loga cada SQL executado em Information, e o ASP.NET Core loga início e fim de request, o que duplica o `UseSerilogRequestLogging`. Tudo isso vai pro Loki e conta na cota do plano gratuito. Um override pra `Warning` em `Microsoft.EntityFrameworkCore` e `Microsoft.AspNetCore` resolve.
3. **Spans soltos no Tempo.** As queries de polling dos processors rodam fora de qualquer span e o Npgsql cria um trace raiz pra cada uma. Requests de `/docs`/`openapi` também entram sem filtro.
4. **Fonte registrada sem uso.** `WebhookCallsProcessing` está no `AddSource` (`Back/Configs/OpenTelemetryConfigs.cs:12`), mas nenhum `ActivitySource` com esse nome existe. As chamadas de webhook de saída não têm span próprio.
5. **Sampler:** `TraceIdRatioBasedSampler` sem `ParentBased`. Com o ratio em 1.0 não faz diferença. Se baixar, ele passa a ignorar a decisão de amostragem de quem chamou.
6. **Nenhuma métrica de negócio.** Não existe nenhum `Meter` próprio. Commands processados, falhas, duração e atraso da fila seriam as métricas mais úteis pra alertas.

## Limpeza (`Back/Back.csproj`)

- **Pacotes não usados:**
  - `Sentry.AspNetCore` e `Sentry.OpenTelemetry` não aparecem em lugar nenhum do código.
  - `OpenTelemetry.Instrumentation.Quartz` não está ligado.
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` também não, e ligar esse duplicaria os spans do Npgsql.
  - `Serilog.Sinks.Seq` não aparece em nenhum appsettings.
- **Versões desalinhadas:**
  - OTel core e exporter estão em 1.15.3; Hosting e as instrumentações em 1.12.0.
  - `Npgsql.OpenTelemetry` 9.0.3 roda sobre Npgsql 10.0.2.
  - Funciona pela unificação do NuGet, mas vale alinhar.
- **`AddMeter("Microsoft.AspNetCore.Hosting")` é redundante** com `AddAspNetCoreInstrumentation()` no .NET 8+. Não faz mal nenhum.
- **A página `Web/app/pages/dev/overview.vue` está desatualizada.** A linha 267 diz que o sink OTLP do Serilog não está configurado, mas ele está (`Back/Configs/HostConfigs.cs:13`).

## Fora do backend

- **Frontend sem telemetria:** o Nuxt não tem nada, e o trace não passa do front pra API. Pra centralizar isso também, o caminho no Grafana Cloud é o **Faro** (monitoramento do navegador).
- **Testar localmente antes de ligar em produção:** rodar `docker run -p 3000:3000 -p 4317:4317 -p 4318:4318 grafana/otel-lgtm` e pôr `Enabled: true` no Development. O `localhost:4317` que já está no appsettings funciona direto com essa imagem.

## Próximos passos sugeridos

1. Corrigir o log de exceções no `ExceptionsMiddleware` (item 1).
2. Configurar as variáveis `OTEL_*` no Railway.
3. Adicionar os overrides de nível de log do Serilog (item 2).
