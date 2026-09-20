# Trocar o disparo dos background processors por Channels do .NET

Hoje os três processadores de fila (`CommandsProcessor`, `DomainEventsProcessor` e
`ReceivedWebhookEventsProcessor`) são `IJob` do Quartz. O `QuartzConfigs` registra um trigger de
intervalo fixo para os dois primeiros (60s em produção) e o
`BackgroundProcessorsTriggerMiddleware` serve de atalho: no fim de cada request, se o `EstudDbContext`
marcou `HasPendingCommands`/`HasPendingDomainEvents`, ele chama `scheduler.TriggerJob(...)` para não
esperar o próximo tick.

O Quartz, nesse arranjo, é **um `PeriodicTimer` com um botão de disparo manual**. A proposta é trocar
esse par (timer + botão) por um `Channel` bounded de capacidade 1 e um `BackgroundService`, e remover
a dependência.

## O que o Quartz entrega e o que está realmente em uso

| Recurso | Em uso? |
|---|---|
| Trigger de intervalo simples | sim — `WithIntervalInSeconds(...).RepeatForever()` |
| `TriggerJob` manual | sim — middleware e re-trigger dentro dos processors |
| Persistência de jobs / triggers | não — RAMJobStore, tudo em memória |
| Clustering entre instâncias | não |
| Cron, calendários, misfire policy | não |
| `[DisallowConcurrentExecution]` | não usado (e a falta dele é um problema, ver abaixo) |
| `OpenTelemetry.Instrumentation.Quartz` | referenciado no `.csproj`, **nunca registrado** no `OpenTelemetryConfigs` |

Os traces dos processors vêm dos `ActivitySource` próprios (`AddSource(CommandsProcessing,
DomainEventsProcessing, WebhookEventsProcessing)`), não do Quartz — a observabilidade não depende dele.

## Os problemas do disparo atual

**Sem coalescing.** Cada request que criou um command dispara uma execução do job. Sob carga, 100
requests/s que criam commands viram 100 execuções, cada uma abrindo um scope, um `EstudDbContext`, uma
conexão e rodando o `UPDATE ... FOR UPDATE SKIP LOCKED` — a esmagadora maioria voltando zero linhas,
porque a primeira execução já drenou a fila. O custo em conexões é proporcional ao tráfego, não ao
trabalho.

**Concorrência ilimitada.** Sem `[DisallowConcurrentExecution]`, não há teto para execuções
simultâneas. O `SKIP LOCKED` garante que ninguém processa o mesmo command duas vezes, mas não limita
quantas conexões a fila consome em pico.

**Custo por request.** `GetScheduler()` e `TriggerJob` são assíncronos e passam pelo lock do job store,
em todo request que tocou em command ou evento.

**O flag mede intenção, não commit.** `HasPendingCommands` é setado dentro de `AddCommand`
(`EstudDbContext.Commands.cs:34`), antes do `SaveChanges`. Se o request falhar depois disso, o flag
continua `true` e dispara o processor à toa. Inofensivo hoje, e a troca por channel não muda isso —
fica registrado como limitação conhecida.

## O desenho com Channel

### O sinal

Capacidade 1 com `DropWrite` é exatamente o debounce que falta: N sinais durante uma execução colapsam
em um único wake-up.

```csharp
public class BackgroundProcessorSignal<TProcessor>
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });

    public void Signal() => _channel.Writer.TryWrite(0);

    public async Task Wait(TimeSpan pollingInterval, CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(pollingInterval);

        try { await _channel.Reader.ReadAsync(cts.Token); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
    }
}
```

O parâmetro genérico existe só para dar uma instância singleton por processador, sem criar três classes
iguais.

**Por que `DropWrite` não perde acordamento:** se o slot está vazio, o sinal é gravado e o próximo
`Wait` retorna na hora. Se o slot está cheio, o sinal é descartado — mas só está cheio porque já existe
um acordamento pendente, que vai encontrar a linha recém-commitada de qualquer jeito. A única forma de
perder trabalho é sinalizar **antes** do commit; o middleware sinaliza depois do `next(context)`, quando
o `SaveChanges` já foi, igual hoje.

### O middleware

Perde a dependência do `ISchedulerFactory` e o `await` no corpo útil:

```csharp
public class BackgroundProcessorsTriggerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        EstudDbContext ctx,
        BackgroundProcessorSignal<CommandsProcessor> commands,
        BackgroundProcessorSignal<DomainEventsProcessor> domainEvents)
    {
        await next(context);

        if (ctx.HasPendingCommands) commands.Signal();
        if (ctx.HasPendingDomainEvents) domainEvents.Signal();
    }
}
```

### Os processors

Viram `BackgroundService`. O polling e o sinal ficam no mesmo `await`, e a concorrência passa a ser 1
por construção — sem precisar de atributo nenhum:

```csharp
public class CommandsProcessor(
    IServiceScopeFactory scopeFactory,
    BackgroundProcessorSignal<CommandsProcessor> signal,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var interval = TimeSpan.FromSeconds(configuration.Jobs.CommandsPollingIntervalInSeconds);

        while (!token.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<EstudDbContext>();

            try { await Process(scope, ctx); }
            catch (Exception ex) { Log.Error(ex, "Erro ao processar commands"); }

            await signal.Wait(interval, token);
        }
    }

    private static async Task Process(IServiceScope scope, EstudDbContext ctx) { /* inalterado */ }
}
```

O `try/catch` em volta do `Process` deixa de ser opcional: uma exceção que escape do `ExecuteAsync` mata
o `BackgroundService` em silêncio e a fila para de ser processada até o próximo deploy. O Quartz logava
e reagendava sozinho.

O `Process` em si — o `while (true)` com `FromSqlRaw`, o `SKIP LOCKED`, as transações por item, o retry
com backoff, os `ActivitySource` — **não muda em nada**.

### Registro

```csharp
builder.Services.AddSingleton<BackgroundProcessorSignal<CommandsProcessor>>();
builder.Services.AddSingleton<BackgroundProcessorSignal<DomainEventsProcessor>>();
builder.Services.AddHostedService<CommandsProcessor>();
builder.Services.AddHostedService<DomainEventsProcessor>();
```

## Arquivos tocados

| Arquivo | Mudança |
|---|---|
| `Back/Background/BackgroundProcessorSignal.cs` | novo |
| `Back/Configs/BackgroundProcessorsConfigs.cs` | novo, substitui o `QuartzConfigs` |
| `Back/Middlewares/BackgroundProcessorsTriggerMiddleware.cs` | sinaliza em vez de agendar |
| `Back/Commands/CommandsProcessor.cs` | `IJob` → `BackgroundService`; o re-trigger das linhas 82-86 vira `signal.Signal()` ou some |
| `Back/DomainEvents/DomainEventsProcessor.cs` | idem; as linhas 79-84 viram `commandsSignal.Signal()` |
| `Back/Webhooks/ReceivedWebhookEventsProcessor.cs` | idem; as linhas 78-82 viram `commandsSignal.Signal()` |
| `Back/Configs/QuartzConfigs.cs` | removido |
| `Back/Extensions/CommandsExtensions.cs` | removido (só tinha `TriggerCommandsProcessorJob`) |
| `Back/Extensions/DomainEventsExtensions.cs` | removido |
| `Back/Program.cs` | `AddQuartzConfigs` → `AddBackgroundProcessorsConfigs` |
| `Back/Back.csproj` | saem `Quartz.Extensions.Hosting` e `OpenTelemetry.Instrumentation.Quartz` |
| `Tests/Base/BackFactory.cs` | `GetSchedulerFactory` → `GetSignal<TProcessor>` |
| `Tests/Base/BackFactory.Background.cs` | `scheduler.TriggerXJob()` → `signal.Signal()`; o polling no banco continua igual |

## Trade-offs

### 1. Perde paralelismo real na drenagem da fila — este é o ponto central

Hoje, dois requests simultâneos disparam duas execuções do `CommandsProcessor` que rodam **de fato em
paralelo**: a primeira reivindica 10 commands (`LIMIT 10` no SQL), a segunda reivindica os 10 seguintes
via `SKIP LOCKED`, e as duas processam ao mesmo tempo. Com um único `BackgroundService`, a drenagem
passa a ser serial.

Isso importa porque os commands são coisas lentas por natureza (e-mail, webhook, HTTP externo). A 300ms
por command, um lote de 10 leva 3s serial; em pico, uma fila de 200 commands leva ~60s em vez de
paralelizar entre execuções. **É uma regressão real de latência sob rajada**, não um detalhe teórico.

Opções, da mais simples à mais elaborada:

- **Aceitar e medir.** Hoje o paralelismo é acidental — depende de quantos requests calharam de criar
  commands no mesmo instante, não de uma decisão. Um worker serial com polling de 60s já é o
  comportamento em regime permanente (fora de pico, um job de cada vez).
- **K workers.** Registrar N instâncias do hosted service. O `SingleReader = true` e a capacidade 1 não
  fazem fan-out: cada worker precisa do próprio sinal, e o middleware sinaliza todos. Funciona, mas o
  debounce por worker fica mais frouxo.
- **Paralelizar dentro do lote.** Manter um worker e processar os 10 commands do lote em paralelo. Isso
  esbarra no fato de o `Process` usar **um único `EstudDbContext` com transação explícita por item** —
  exigiria um scope por command, uma refatoração bem maior do que a troca de disparo.

Recomendação: começar com 1 worker por fila, medir, e só então decidir entre K workers e paralelismo
intra-lote. Se a decisão for K workers desde o início, a complexidade extra já come boa parte do ganho
de simplicidade da proposta.

### 2. Ganha coalescing e teto de concorrência

O espelho do item anterior. Sob 100 req/s criando commands, o disparo passa de 100 execuções (100
scopes, 100 conexões, 100 queries quase sempre vazias) para exatamente um acordamento. A pressão no pool
de conexão deixa de ser função do tráfego e passa a ser função do trabalho real.

### 3. Menos código e menos dependência

Somem `QuartzConfigs.cs`, dois arquivos de extension, dois pacotes NuGet (um deles referenciado sem
nunca ter sido registrado) e o acoplamento do middleware ao `ISchedulerFactory`. Entra uma classe de ~20
linhas. O `Process` de cada processador — que é onde mora toda a complexidade de verdade — fica
intocado.

### 4. Fecha a porta para features do Quartz que hoje não são usadas

Cron, persistência de trigger, calendários de feriado, misfire policy, coordenação entre instâncias.
Nada disso está em uso, mas se aparecer a necessidade de "rodar tal coisa toda segunda às 3h", o Quartz
volta — ou entra um `PeriodicTimer` com cálculo de próxima ocorrência, que é pior. Vale pesar se há algo
nesse horizonte antes de remover o pacote. Uma saída intermediária: manter o Quartz para jobs agendados
de verdade e usar channel só para o disparo reativo das filas — mas aí não há economia de dependência,
só o coalescing.

### 5. Shutdown fica um pouco menos garantido

Hoje `AddQuartzHostedService(q => q.WaitForJobsToComplete = true)` espera os jobs em voo terminarem. Com
`BackgroundService`, como o `Process` não observa o `CancellationToken`, o loop termina a drenagem
corrente e sai na volta do `while` — paridade aproximada, limitada pelo `ShutdownTimeout` do host (30s
por padrão). Se uma drenagem puder passar disso, o host aborta no meio e a linha fica reivindicada (ver
item 7). Mitigação: passar o token para o `while` externo, ou reduzir o `LIMIT` do lote.

### 6. Tratamento de exceção passa a ser responsabilidade nossa

Já citado acima, mas conta como trade-off: o Quartz capturava exceção de job, logava e seguia agendando.
Um `BackgroundService` que deixa a exceção escapar morre calado, e a fila para até o próximo deploy. O
`try/catch` no loop é obrigatório e precisa de teste.

### 7. Não muda nada sobre multi-instância nem sobre claims órfãos

Duas preocupações comuns que a troca **não** afeta, em nenhuma direção:

- **Multi-instância.** O `TriggerJob` sobre RAMJobStore só acorda a instância local — o sinal já é
  in-process hoje. O channel é exatamente equivalente. Se um dia o disparo precisar cruzar instâncias, a
  resposta natural é `LISTEN`/`NOTIFY` do Postgres, e um worker com channel é mais fácil de plugar nisso
  do que um `IJob` (o `NOTIFY` vira um `Signal()`).
- **Claims órfãos.** Se o processo morre no meio de um lote, as linhas ficam com `processor_id` setado e
  `status` 1 ou 2 para sempre, porque as queries filtram por `processor_id IS NULL AND status = 0`. O
  buraco existe hoje e continua existindo. A correção usual é uma coluna `claimed_at` e um
  `OR (status = 2 AND claimed_at < NOW() - INTERVAL '5 minutes')` — fora do escopo deste plano.

## Ordem de implementação

1. `BackgroundProcessorSignal<T>` e o registro dos singletons, ainda com o Quartz no lugar.
2. `CommandsProcessor` → `BackgroundService`, com o `Process` intocado; remover o trigger do Quartz para
   ele. Rodar os testes de integração que dependem de `AwaitCommandsProcessing`.
3. Mesmo para o `DomainEventsProcessor`.
4. Adaptar `BackFactory.Background.cs` e `BackFactory.GetSchedulerFactory`.
5. Simplificar o middleware.
6. Remover `QuartzConfigs.cs`, os dois arquivos de extension e os dois pacotes do `.csproj`.

O passo 2 sozinho já é entregável e reversível: dá para ter uma fila em channel e a outra em Quartz
convivendo, o que torna o rollback barato se a serialização da drenagem apertar em produção.

## Fora do escopo, mas achado no caminho

**`ReceivedWebhookEventsProcessor` nunca roda.** É um `IJob` sem `AddJob`/`AddTrigger` no
`QuartzConfigs`, e `WebhookEventsPollingIntervalInSeconds` está nos três `appsettings` sem nenhum
leitor. Webhooks recebidos não são processados hoje. A migração para `BackgroundService` acidentalmente
corrige isso ao registrar o hosted service — o que significa que, no deploy, uma fila parada desde
sempre começa a drenar de uma vez. Conferir o que há em `received_webhook_events` com `status = 0`
**antes** de subir.

**Sem reaper de claim órfão**, detalhado no trade-off 7.
