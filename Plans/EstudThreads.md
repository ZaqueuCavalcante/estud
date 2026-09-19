# Threads de entrega: nota, feedback e reenvio

Hoje a correção é uma via de mão única: o aluno entrega, o professor digita um número e a entrega
vira `Finalized`. Não tem como explicar a nota, pedir um ajuste nem responder.

Este plano transforma cada `ClassActivityWork` numa **thread**: uma linha do tempo com as versões da
entrega, as avaliações do professor e os comentários dos dois lados, todos em texto rico. A conversa
pode continuar indefinidamente, e o professor pode pedir alterações para o aluno mandar uma nova
versão.

É a fase 3 do `Plans/RichEditor.md` ("comentários sem menção"), ampliada com avaliação e reenvio.
Menção e notificação por inscrição continuam em `Plans/RichEditor.md` (fase 4) e
`Plans/Mentions.md`. Este plano deixa o terreno pronto para os dois e muda o nome da entidade que
eles citam (ver "Impacto nos outros planos").

## Ponto de partida

O que existe, incluindo o trabalho ainda não commitado de entrega com texto rico:

- `ClassActivityWork` (`Back/Domain/Classes/ClassActivityWork.cs`): `Content` (markdown), `Note`
  (`decimal`, default 0) e `Status`. `Deliver(content)` sobrescreve o conteúdo e marca `Delivered`,
  **em qualquer status**. Isso inclui `Finalized`: o aluno consegue reenviar depois da nota, e a nota
  antiga continua valendo. `AddNote(note)` marca `Finalized`.
- `ClassActivityWorkStatus`: `Pending = 0`, `Delivered = 1`, `InReview = 2` (só prova vencida,
  calculado em `ClassActivity.GetWorkStatus`), `Finalized = 3`.
- Entregas: `POST students/activities/{id}/works` e `POST students/activities/{id}/works/files`
  (URL pré-assinada, container `ClassActivityWorkFiles`).
- Nota: `PUT teachers/activities/{activityId}/works/{workId}/note` (`AddActivityNote`).
- Aproveitamento (`ClassGrade.Performance`) soma `Note × Weight` de **todas** as atividades,
  qualquer que seja o status. Uma entrega pendente já conta como 0.
- Front: `DetailTeacher.vue` lista as entregas com `AddNoteModal`. `DetailStudent.vue` mostra a
  entrega e abre `CreateWorkModal`, que ainda é o formulário de link. `RichEditor.vue` já sabe
  subir imagem e PDF, e `MarkdownContent.vue` exibe markdown com `UEditor` em modo leitura.
- Notificação: domain event → `IDomainEventHandler` → `ctx.AddCommand(...)` → handler cria
  `Notification` + `UserNotification`. O `UpdateClassActivity` é o exemplo mais recente.

## Conceito

A thread é a linha do tempo de **uma** entrega (um aluno × uma atividade). Ela tem três tipos de
item:

| Item | Quem cria | Conteúdo | Efeito no status |
|---|---|---|---|
| **Entrega** (versão) | aluno | markdown obrigatório | → `Delivered` |
| **Avaliação** | professor | nota e/ou feedback em markdown + decisão | → `Finalized` ou `ChangesRequested` |
| **Comentário** | ambos | markdown obrigatório | nenhum |

A avaliação é separada do comentário porque é ela que **muda o estado** e **registra a nota**. Com
isso, a thread serve também de histórico de notas ("tirou 6, refez, tirou 8,5"), que hoje se perde a
cada `AddNote`.

### Ciclo de vida

```
            entrega                 avaliação "finalizar"
Pending ─────────────▶ Delivered ─────────────────────────▶ Finalized
                        ▲    │                                  │
             nova versão│    │ avaliação "pedir alterações"     │ avaliação "pedir alterações"
                        │    ▼                                  │ (reabre)
                   ChangesRequested ◀───────────────────────────┘
```

- O aluno envia uma nova versão em `Pending`, `Delivered` (antes da avaliação, substitui a anterior
  como versão mais recente) e `ChangesRequested`. Em `Finalized`, **não**. Para mudar alguma coisa
  depois da nota, ele comenta e o professor decide se reabre. Isso fecha o buraco de hoje, em que o
  reenvio depois da nota passa despercebido.
- O professor avalia em qualquer status com pelo menos uma versão entregue. Em prova, que não aceita
  entregas, a avaliação só pode finalizar.
- O comentário vale em qualquer status, de qualquer lado, e é o que permite a conversa "infinita".

Status novo, com o próximo valor livre:

```csharp
[Description("Alterações solicitadas")]
ChangesRequested = 4,
```

### A nota

A nota vigente continua em `ClassActivityWork.Note`, porque é dela que o aproveitamento e as
listagens leem. Cada avaliação grava a nota que deu na própria linha, e isso é o histórico.

- "Finalizar" exige nota.
- "Pedir alterações" aceita nota opcional: é a nota provisória, útil quando o professor quer dizer
  "vale 6 do jeito que está, refaz que sobe". Sem nota, a vigente não muda.
- Como o aproveitamento já conta tudo, qualquer que seja o status, a nota provisória conta até ser
  substituída. É coerente com a regra de hoje, em que o pendente conta 0. A alternativa (ignorar
  `ChangesRequested` no cálculo) está em "Pontos em aberto".

### Atraso

`Deliver` não checa o prazo hoje, e este plano não muda isso: reenviar a pedido do professor depois
do prazo tem que ser possível. O atraso vira **informação**, calculado na leitura: a versão cujo
`CreatedAt` é depois de `DueDate` + `DueHour` recebe `IsLate = true`, e a UI mostra "Entregue com
atraso". Não precisa de coluna.

## Banco

### Entidade nova

Em `Back/Domain/Classes/`:

```csharp
public class ClassActivityWorkEntry : DomainEntity
{
    public int Id { get; set; }
    public int ClassActivityWorkId { get; set; }
    public ClassActivityWork? Work { get; set; }
    public int AuthorUserId { get; set; }
    public EstudUser? Author { get; set; }
    public ClassActivityWorkEntryType Type { get; set; }
    public string? Content { get; set; }
    public decimal? Note { get; set; }
    public ClassActivityWorkReviewDecision? Decision { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public enum ClassActivityWorkEntryType
{
    [Description("Entrega")]
    Submission = 0,

    [Description("Avaliação")]
    Review = 1,

    [Description("Comentário")]
    Comment = 2,
}

public enum ClassActivityWorkReviewDecision
{
    [Description("Finalizada")]
    Finalized = 0,

    [Description("Alterações solicitadas")]
    ChangesRequested = 1,
}
```

**Uma tabela só**, e não uma por tipo. A thread é lida sempre inteira e em ordem cronológica, e os
três tipos têm as mesmas colunas de autoria, conteúdo e datas. Tabelas separadas obrigariam a um
`UNION` com ordenação em toda leitura, e cada feature futura (menção, reação, "lido") teria que
apontar para três lugares. `Note` e `Decision` são nulos fora da avaliação, e um `CHECK` no config
garante isso.

**Autor como `UserId`**, não `StudentId`/`TeacherId`, pelo motivo do `RichEditor.md`: a thread
mistura os papéis, e a menção aponta para pessoa. O papel exibido ("Professor"/"Aluno") sai do
`EstudUser.Type`.

`DomainEntity` porque é da criação de um item que nascem as notificações (ver "Notificações").

`ClassActivityWorkEntryConfig`:

- índice `(class_activity_work_id, created_at)`, que é a leitura da thread;
- `content` com `varchar(10000)`, o mesmo limite do plano de aula e da entrega;
- FK para `users` com `Restrict`: usuário com histórico não se apaga.

### Mudanças em `ClassActivityWork`

```csharp
public List<ClassActivityWorkEntry> Entries { get; set; } = [];
public DateTime? LastEntryAt { get; set; }
```

`LastEntryAt` é desnormalizado para a lista do professor ordenar por "atividade recente" e mostrar
"nova mensagem" sem subconsulta por linha.

`Content` continua existindo, como **cópia da última versão**. Assim, as leituras atuais
(`GetStudentClassActivity`, `GetTeacherClassActivity`) não mudam, e a thread é quem guarda o
histórico. A alternativa é derivar a última versão das entries, mas isso põe subconsulta em
listagens que hoje são simples, sem ganho real.

Os métodos de domínio concentram as regras:

```csharp
public OneOf<ClassActivityWorkEntry, EstudError> Deliver(int authorUserId, string content)
public OneOf<ClassActivityWorkEntry, EstudError> Review(int authorUserId, decimal? note, string? feedback,
    ClassActivityWorkReviewDecision decision, int lastSeenSubmissionId, bool acceptsWorks)
public ClassActivityWorkEntry Comment(int authorUserId, string content)
```

- `Deliver` recusa `Finalized` com `ClassActivityWorkAlreadyFinalized`.
- `Review` recusa sem entrega (quando a atividade aceita entregas), recusa `Finalized` sem nota,
  recusa `ChangesRequested` sem feedback ("pedir alteração" sem dizer o quê é inútil para o aluno) e
  recusa `ChangesRequested` em prova.
- `AddNote` sai, substituído por `Review`.

### Concorrência

O aluno pode mandar uma versão nova enquanto o professor está escrevendo a avaliação da anterior. A
avaliação carrega o `lastSeenSubmissionId`, que é a versão que o professor tinha na tela. Se existir
uma mais nova, a avaliação é recusada com `ClassActivityWorkHasNewSubmission`, e a UI recarrega a
thread sem perder o texto digitado. Para prova, que não tem versão, o campo vai nulo.

### Migration

1. Cria `class_activity_work_entries` e a coluna `last_entry_at`.
2. Backfill: toda entrega com `content` não nulo ganha uma entry `Submission` com o conteúdo, autor =
   `students.user_id` e `created_at` = data da migration (não existe data de entrega hoje).
   `last_entry_at` recebe o mesmo valor.
3. **Nota antiga não vira entry `Review`**, porque não se sabe quem deu. O cabeçalho da thread
   mostra a nota vigente a partir de `ClassActivityWork.Note`, então nada se perde na tela.

Esse passo roda depois da migração `Link` → `Content` que está no diff atual.

## Backend

A convenção do projeto é endpoint por papel (`teachers/...`, `students/...`), e ela é mantida.
O `RichEditor.md` propunha rotas únicas para os dois papéis, mas a checagem de acesso fica mais
simples e óbvia com um endpoint por papel, e as policies continuam por `UserType`. O que é igual nos
dois lados fica no domínio e num mapper compartilhado.

### Features novas e alteradas

| Feature | Rota | Papel | O que faz |
|---|---|---|---|
| `GetTeacherClassActivityWork` | `GET teachers/activities/{activityId}/works/{workId}` | professor | thread completa + navegação anterior/próximo |
| `ReviewClassActivityWork` | `POST teachers/activities/{activityId}/works/{workId}/reviews` | professor | nota + feedback + decisão |
| `CreateTeacherWorkComment` | `POST teachers/activities/{activityId}/works/{workId}/comments` | professor | comentário |
| `CreateTeacherWorkFile` | `POST teachers/activities/{activityId}/works/{workId}/files` | professor | URL pré-assinada para imagem/PDF no feedback |
| `GetStudentClassActivityWork` | `GET students/activities/{activityId}/work` | aluno | a thread da própria entrega |
| `CreateClassActivityWork` *(alterada)* | `POST students/activities/{activityId}/works` | aluno | passa a criar uma versão |
| `CreateStudentWorkComment` | `POST students/activities/{activityId}/work/comments` | aluno | comentário |
| `CreateClassActivityWorkFile` *(existente)* | `POST students/activities/{activityId}/works/files` | aluno | sem mudança; serve também ao comentário |
| `UpdateWorkComment` | `PUT activities/works/comments/{entryId}` | autor | edita o próprio comentário |
| `DeleteWorkComment` | `DELETE activities/works/comments/{entryId}` | autor | soft delete do próprio comentário |
| `AddActivityNote` *(removida)* | — | — | substituída por `ReviewClassActivityWork` |

Regras que valem para todos:

- **Acesso do professor**: vinculado à turma em `ClassTeachers`, turma da instituição do
  `ctx.RequestUser`. Hoje isso está repetido em `AddActivityNoteService`. Vira helper no
  `EstudDbContext.Teachers.cs` (`TeacherCanAccessWork(teacherId, activityId, workId)`), que também é
  o que o `Mentions.md` vai precisar no handler.
- **Acesso do aluno**: dono da entrega (`ClassActivityWork.StudentId`), matriculado na turma.
- **Editar e excluir** só o próprio **comentário**. Versão entregue e avaliação não se editam nem se
  apagam: uma é o que foi entregue, a outra é registro de nota. Corrigir uma avaliação é fazer outra.
  As duas features de autor ficam em `Back/Features/Cross/`, com `AddEstudPolicy(Nome)` (basta estar
  logado) e checagem `AuthorUserId == ctx.RequestUser.Id` no service.
- Comentário excluído some da thread como "Comentário removido", mantendo a posição. O conteúdo é
  apagado de fato (`Content = null`), não só escondido.
- Conteúdo vazio conta como vazio: o editor serializa documento vazio como `""`, então
  `IsEmpty()` basta, como já acontece no plano de aula.
- Arquivo do professor vai no mesmo container `ClassActivityWorkFiles`, com o mesmo formato de path
  (`{institutionId}/{classId}/{activityId}/{studentId}/{ulid}.{ext}`). Os limites e os tipos são os
  do `CreateClassActivityWorkFileService`, que viram um dicionário compartilhado em vez de cópia.

### `ReviewClassActivityWorkIn`

```csharp
public class ReviewClassActivityWorkIn : IApiDto<ReviewClassActivityWorkIn>
{
    public decimal? Note { get; set; }
    public string? Feedback { get; set; }
    public ClassActivityWorkReviewDecision Decision { get; set; }
    public int? LastSeenSubmissionId { get; set; }
}
```

Validação: nota entre 0 e 10 quando informada (`InvalidStudentClassNote`, que já existe); feedback
até 10000 caracteres. As regras de combinação (finalizar exige nota, pedir alteração exige feedback)
ficam no domínio, porque dependem do tipo de atividade.

### Saída da thread

As duas rotas `GET` devolvem o mesmo formato, montado por um mapper compartilhado:

```csharp
public class ClassActivityWorkThreadOut
{
    public int WorkId { get; set; }
    public ClassActivityWorkStatus Status { get; set; }
    public decimal Note { get; set; }
    public ThreadPersonOut Student { get; set; }
    public ThreadActivityOut Activity { get; set; }
    public List<ThreadEntryOut> Entries { get; set; }
    public int? PreviousWorkId { get; set; }
    public int? NextWorkId { get; set; }
}

public class ThreadEntryOut
{
    public int Id { get; set; }
    public ClassActivityWorkEntryType Type { get; set; }
    public ThreadPersonOut Author { get; set; }
    public string? Content { get; set; }
    public decimal? Note { get; set; }
    public ClassActivityWorkReviewDecision? Decision { get; set; }
    public int? Version { get; set; }
    public bool IsLate { get; set; }
    public bool CanEdit { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}
```

- `Version` (1, 2, 3...) é calculado na leitura, contando as entregas em ordem.
- `CanEdit` vem pronto do backend (autor + comentário + não excluído). O front não reimplementa a
  regra.
- `PreviousWorkId`/`NextWorkId` só no professor, na ordem alfabética dos alunos, a mesma da lista.
  É o que permite corrigir a turma inteira sem voltar à listagem.

### `GetTeacherClassActivity` (lista)

Cada item de `Works` ganha `EntriesCount`, `LastEntryAt` e `AwaitingTeacher`. `AwaitingTeacher` é
verdadeiro quando o status é `Delivered` **ou** quando o último item da thread é do aluno, ou seja,
quando o aluno comentou depois da última fala do professor. É o "tem coisa para você aqui" da
listagem, e dispensa controle de lido por enquanto. `DeliveredWorks` passa a contar também
`ChangesRequested` como entregue.

### Erros novos

Em `Back/Errors/EstudErrors.Classes.cs`: `ClassActivityWorkAlreadyFinalized`,
`ClassActivityWorkHasNoSubmission`, `ClassActivityWorkHasNewSubmission`,
`InvalidClassActivityWorkReview` (combinação de decisão, nota e feedback),
`InvalidClassActivityWorkComment`, `ClassActivityWorkEntryNotFound` e
`ClassActivityWorkEntryNotEditable`.

### Policies

`Policies.Teachers.cs`: `GetTeacherClassActivityWork`, `ReviewClassActivityWork`,
`CreateTeacherWorkComment`, `CreateTeacherWorkFile`, todas com `UserType.Teacher`.
`Policies.Students.cs`: `GetStudentClassActivityWork` e `CreateStudentWorkComment`, com
`UserType.Student`. `Policies.Cross.cs`: `UpdateWorkComment` e `DeleteWorkComment`, só logado.
`AddActivityNote` sai.

## Notificações

Regras simples, sem inscrição, que o `Mentions.md` depois substitui pela versão com `Reason` e
`NotificationSubscription`:

| Evento | Quem recebe | Texto |
|---|---|---|
| Avaliação finalizando | aluno | "Sua entrega de *X* recebeu nota 8,5" |
| Avaliação pedindo alterações | aluno | "O professor pediu alterações em *X*" |
| Comentário do professor | aluno | "Novo comentário em *X*" |
| Nova versão depois de `ChangesRequested` | autor da última avaliação | "Maria enviou uma nova versão de *X*" |
| Comentário do aluno | professores que já falaram na thread; se nenhum, todos da turma | "Maria comentou em *X*" |

A primeira entrega **não** notifica o professor: com 40 alunos, seriam 40 notificações por
atividade. A lista com `AwaitingTeacher` resolve esse caso.

Fluxo: criar a entry dispara `ClassActivityWorkEntryCreatedDomainEvent(Uid)`, cujo handler enfileira
`CreateWorkEntryNotificationCommand(entryId)`. O command decide destinatários e texto pelo tipo.
Ele também **checa o acesso de novo**, porque o professor pode ter sido desvinculado da turma entre
o evento e o processamento.

`NotificationType`, alinhado com a numeração reservada no `Mentions.md`:

```csharp
WorkCommentMention = 200,
WorkComment = 201,
WorkReviewed = 202,
WorkResubmitted = 203,
```

Os links apontam para a tela do professor (`/classes/{c}/activities/{a}/works/{w}`) ou do aluno
(`/classes/{c}/activities/{a}`). Por isso, quando os destinatários têm papéis diferentes, o command
gera uma `Notification` por papel.

## Frontend

### Componentes

| Componente | Papel |
|---|---|
| `activities/WorkThread.vue` | recebe a thread e renderiza a linha do tempo; usado pelos dois lados |
| `activities/WorkThreadEntry.vue` | um item: entrega, avaliação ou comentário |
| `activities/WorkComposer.vue` | editor + ações, com os modos que o papel e o status permitem |
| `activities/WorkReviewForm.vue` | nota + feedback + as duas ações do professor |
| `pages/classes/[classId]/activities/[activityId]/works/[workId].vue` | tela de correção do professor |

`RichEditor.vue` ganha a prop `compact`, que troca o `min-h-64` por uma altura menor e cresce com o
texto. Menção continua com `:mention="false"` até a fase de menção.

`CreateWorkModal.vue` (link) e `AddNoteModal.vue` saem. A entrega e a nota passam a acontecer dentro
da thread, sem modal.

### Tela do professor — correção

Hoje o professor dá nota por modal, direto da lista. Com feedback em texto rico, o modal fica
apertado, e o professor precisa ver a entrega enquanto escreve. Por isso a correção ganha **rota
própria**. Ela também é o destino dos links das notificações, e o Tiptap fica num chunk separado,
como já acontece no planejamento de aula.

```
┌──────────────────────────────────────────────────────────────┐
│ Turmas / Detalhes / Atividade / Maria Souza                  │
├──────────────────────────────────────────────────────────────┤
│ (avatar) Maria Souza   [Alterações solicitadas]   Nota 6,0   │
│ Trabalho 1 · N1 · Peso 40%            ‹ Anterior  Próximo ›  │
├──────────────────────────────────────────────────────────────┤
│ ● Entrega · versão 1 · 12/09 22:14                           │
│   ┌──────────────────────────────────────────────┐           │
│   │ (conteúdo recolhido — "Ver versão 1")        │           │
│   └──────────────────────────────────────────────┘           │
│ ● Avaliação · Prof. João · 13/09        [Pediu alterações]   │
│   Nota provisória 6,0                                        │
│   │ O diagrama não tem as cardinalidades...                  │
│ ● Maria comentou · 13/09                                     │
│   │ Professor, é 1:N ou N:N entre livro e autor?             │
│ ● Prof. João comentou · 13/09                                │
│ ● Entrega · versão 2 · 15/09 09:02          [Nova]           │
│   ┌──────────────────────────────────────────────┐           │
│   │ conteúdo completo                            │           │
│   └──────────────────────────────────────────────┘           │
├──────────────────────────────────────────────────────────────┤
│ [ Comentar | Avaliar ]                                       │
│ ┌ Avaliar ────────────────────────────────────────────────┐  │
│ │ Nota [ 8,5 ]                                            │  │
│ │ Feedback (editor rico)                                  │  │
│ │                     [Pedir alterações]  [Finalizar ✓]   │  │
│ └─────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

Decisões de UX:

- **A versão mais recente aparece aberta, e as anteriores ficam recolhidas.** A linha do tempo
  conta a história, mas o que o professor corrige é a última versão.
- **Avaliação e comentário no mesmo composer**, alternados por `UTabs`. A aba inicial é "Avaliar"
  quando o status é `Delivered` e "Comentar" nos outros casos.
- **Os dois botões da avaliação dizem o que acontece**: "Pedir alterações" (neutro) e "Finalizar"
  (primário). Um select de decisão esconderia a consequência.
- Com status `Finalized`, a aba "Avaliar" mostra "Reavaliar", e o texto de apoio explica que a nota
  nova substitui a atual.
- **Anterior/Próximo** preservam o fluxo de corrigir a turma em sequência. Depois de finalizar, um
  toast oferece "Ir para o próximo".
- O input de nota reaproveita a máscara do `AddNoteModal` (`onNoteKeydown`/`onNoteInput`), que vira
  um componente `NoteInput.vue`.
- Na listagem (`DetailTeacher.vue`), cada linha vira link para a tela de correção e mostra a
  contagem de mensagens e um ponto "aguardando você" (`AwaitingTeacher`). Filtros em `UTabs`: Todas
  · Aguardando · Alterações solicitadas · Finalizadas · Sem entrega.

### Tela do aluno

A seção "Minha entrega" do `DetailStudent.vue` passa a ser a thread, na mesma página. Para o aluno
só existe uma entrega por atividade, então não vale uma rota nova.

- `Pending`: sem thread, e o composer aparece aberto no modo "Entregar", com o `RichEditor` com
  upload.
- `ChangesRequested`: um `UAlert` no topo, "O professor pediu alterações", com link para a
  avaliação. O composer oferece "Enviar nova versão" (primário) e "Comentar".
- `Delivered`: o composer oferece "Comentar" e "Enviar nova versão".
- `Finalized`: só "Comentar". A nota aparece em destaque no cabeçalho da seção.
- Em prova, não existe entrega: só a avaliação do professor e os comentários.

### Comportamentos comuns

- **Rascunho**: o texto do composer é salvo em `localStorage` por `workId` + modo, e limpo ao
  enviar. Feedback longo não pode sumir por um F5.
- `Ctrl/⌘ + Enter` envia. Enviar fica desabilitado com upload em andamento (`v-model:uploading` já
  existe no `RichEditor`) ou com conteúdo vazio.
- Depois de enviar, `refresh()` da thread e rolagem até o item novo. Não há atualização otimista:
  a ordem e a versão vêm do servidor.
- `ClassActivityWorkHasNewSubmission`: toast "O aluno enviou uma nova versão", `refresh()` da
  thread e o texto digitado fica no composer.
- Comentário próprio tem menu (`UDropdownMenu`) com Editar/Excluir. Editar troca o item pelo editor
  no mesmo lugar. Excluir pede confirmação.
- Rótulo de papel ao lado do nome ("Professor") e datas relativas com `title` absoluto.
- Status novo em `Web/app/utils/classes.ts`: `ChangesRequested: 'Alterações solicitadas'`, cor
  `warning`. `InReview` passa a usar outra cor para não se confundir com ele.

### Custo de render

Cada item exibido com `MarkdownContent` é uma instância de Tiptap. Uma thread de 30 itens são 30
editores. Mitigações, na ordem:

1. versões antigas recolhidas não montam o editor;
2. comentários curtos e sem formatação (a maioria) podem sair num `<p>` simples, e o
   `MarkdownContent` fica para quando houver marcação;
3. se ainda pesar, uma renderização de markdown sem editor (`@nuxtjs/mdc`) para os itens sem
   menção. É o que o `RichEditor.md` já prevê.

## Testes

Integração, um arquivo por feature, com as regions na ordem do CLAUDE.md e o cenário montado pelos
endpoints. Helpers novos no `TestsHttpClient` (`.Teachers` e `.Students`):
`GetTeacherClassActivityWork`, `ReviewClassActivityWork`, `CreateTeacherWorkComment`,
`CreateTeacherWorkFile`, `GetStudentClassActivityWork`, `CreateStudentWorkComment`,
`UpdateWorkComment` e `DeleteWorkComment`. Os testes de `AddActivityNote` migram para
`ReviewClassActivityWork`, e quem usava `AddActivityNote` como arrange passa a usar o helper novo.

Cenários que importam:

- **Ciclo completo**: entrega → pedir alterações com nota 6 → aluno comenta → professor comenta →
  nova versão → finalizar com 8,5. A thread tem 6 itens na ordem, versões 1 e 2, nota vigente 8,5,
  status `Finalized`.
- Nova versão em `Finalized` → `ClassActivityWorkAlreadyFinalized`. Depois de "pedir alterações"
  sobre uma finalizada, a nova versão é aceita.
- Avaliar com `LastSeenSubmissionId` antigo → `ClassActivityWorkHasNewSubmission`.
- Finalizar sem nota, pedir alterações sem feedback, pedir alterações em prova →
  `InvalidClassActivityWorkReview`. Avaliar entrega sem versão → `ClassActivityWorkHasNoSubmission`.
- Pedir alterações sem nota mantém a nota vigente. Com nota, atualiza a nota e o aproveitamento em
  `GetStudentClassActivities`.
- Professor de outra turma não lê, não avalia e não comenta (`TeacherNotAssignedToClass`). Aluno não
  lê a thread de colega (a rota do aluno não recebe `workId`, então o teste é que ele só vê a
  própria).
- Editar e excluir comentário de outra pessoa → `ClassActivityWorkEntryNotEditable`. Editar entrega
  ou avaliação pelo endpoint de comentário → mesmo erro. Comentário excluído volta com
  `Deleted = true` e `Content = null`.
- Entrega depois do prazo volta com `IsLate = true`.
- `GetTeacherClassActivity`: `AwaitingTeacher` fica verdadeiro depois do comentário do aluno e
  falso depois da resposta do professor.
- Notificações, com `AwaitCommandsProcessing`: o aluno recebe `WorkReviewed` e `WorkComment`; quem
  avaliou recebe `WorkResubmitted`; comentário do aluno sem professor na thread notifica todos da
  turma; a primeira entrega não notifica ninguém; o autor nunca recebe a notificação do próprio item.
- Unidade (`ClassActivityWork`): a tabela de transições de status e as regras de `Review`.

## Faseamento

Cada fase vai para produção sozinha.

1. **Histórico de versões.** Entidade, migration com backfill, `Deliver` criando `Submission` e
   recusando `Finalized`, `GetStudentClassActivityWork`/`GetTeacherClassActivityWork` só leitura.
   No front, a entrega com `RichEditor`, que conclui o trabalho em andamento e substitui o
   `CreateWorkModal`, e a thread mostrando só as versões.
2. **Avaliação com feedback.** `ReviewClassActivityWork`, status `ChangesRequested`, remoção do
   `AddActivityNote`, tela de correção com Anterior/Próximo, `CreateTeacherWorkFile`. Isso já
   resolve "nota com explicação" e "refaz".
3. **Comentários.** Criar, editar e excluir nos dois lados, `AwaitingTeacher` e filtros na
   listagem.
4. **Notificações simples**, pela tabela acima.
5. **Menções**, que é a fase 4 do `RichEditor.md`, e depois o `Mentions.md` (inscrição, motivo,
   caixa agrupada, e-mail).

## Impacto nos outros planos

- `RichEditor.md` e `Mentions.md` falam em `ClassActivityWorkComment` e
  `ClassActivityWorkCommentMention`. Com a tabela única, os nomes passam a ser
  `ClassActivityWorkEntry` e `ClassActivityWorkEntryMention`. Um ganho é que menção passa a valer
  também no feedback da avaliação ("@Maria @João, o grupo todo refaz a parte 2").
- O `NotificationSubjectType.ClassActivityWork` do `Mentions.md` casa com esta thread: o assunto é
  o `workId`, e abrir a tela de correção ou a seção do aluno marca como lido por assunto.
- O item do TODO "notificar aluno quando o professor adicionar nota ou comentar, mesmo sem menção" é
  a fase 4 daqui.

## Pontos em aberto

- **Nota provisória no aproveitamento.** O plano conta a nota de `ChangesRequested` porque o cálculo
  atual já conta tudo. A alternativa é o aproveitamento ignorar entregas em `ChangesRequested`, para
  o aluno não ver um aproveitamento "punido" por algo que ele ainda vai refazer. Decisão pedagógica.
- **Pedir alterações exige feedback?** O plano exige. Se incomodar ("refaz, você sabe o porquê"),
  basta afrouxar a regra no domínio.
- **Prazo para reenvio.** Hoje é indefinido. Pode fazer sentido o professor informar "reenviar até
  dd/mm" na avaliação. Isso seria um `ResubmitUntil` na entry, e fica para depois se alguém pedir.
- **Edição com janela de tempo.** O plano deixa editar o próprio comentário sempre, marcando
  "editado". Limitar a, digamos, 15 minutos evita reescrever a conversa depois da resposta do outro
  lado.
- **Gestor lendo threads.** Coordenação vendo feedback dos professores é um pedido provável. Não
  entra agora; seria um terceiro `GET` com policy de gestor.
- **Webhook** para `WorkReviewed`: o `ClassActivityWorkEntryCreatedDomainEvent` já é domain event,
  então entra de graça se o sistema de webhooks listar eventos de domínio.

## Fora do escopo

- Entrega em grupo (ver `RichEditor.md`).
- Reações, respostas aninhadas, citação de trecho da entrega ("comentário inline" na linha X).
- Rubrica (critérios com pontuação parcial).
- Comparação visual entre versões (diff).
- Tempo real na thread. Por ora é `refresh()` ao enviar, mais a notificação.
