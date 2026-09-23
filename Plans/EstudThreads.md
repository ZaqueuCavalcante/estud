# Threads de entrega: nota, feedback e reenvio

Hoje a correção é uma via de mão única: o aluno entrega, o professor digita um número e a entrega
vira `Finalized`. Não tem como explicar a nota, pedir um ajuste nem responder.

Este plano transforma cada `ClassActivityWork` num **card**, no estilo do Basecamp: um por aluno ×
atividade, que existe desde que a atividade é criada, com uma linha do tempo de tudo o que aconteceu
nele. Entram ali as versões da entrega, as avaliações do professor e os comentários dos dois lados,
todos em texto rico. A conversa pode começar por qualquer lado, antes ou depois da entrega, e
continuar indefinidamente. O professor pode pedir alterações para o aluno mandar uma nova versão.

O `ClassActivityWork` passa a guardar só o **estado** do card: de quem é, de qual atividade, o
status e a nota vigente. O conteúdo vive inteiro na linha do tempo.

É a fase 3 do `Plans/RichEditor.md` ("comentários sem menção"), ampliada com avaliação e reenvio.
Menção e notificação por inscrição continuam em `Plans/RichEditor.md` (fase 4) e
`Plans/Mentions.md`. Este plano deixa o terreno pronto para os dois e muda o nome da entidade que
eles citam (ver "Impacto nos outros planos").

## Ponto de partida

O que existe, já com a entrega em texto rico (`fb02f265`), que trocou o link por `Content`:

- `ClassActivityWork` (`Back/Domain/Classes/ClassActivityWork.cs`): `Content` (markdown, até 10000
  caracteres), `Note` (`decimal`, default 0) e `Status`. `Deliver(content)` sobrescreve o conteúdo e
  marca `Delivered`, **em qualquer status**. Isso inclui `Finalized`: a tela do aluno não oferece
  editar depois da nota, mas a API aceita, e a nota antiga continua valendo. `AddNote(note)` marca
  `Finalized`. A entidade não é `DomainEntity`.
- O `ClassActivityWork` já nasce `Pending` para cada aluno da turma quando a atividade é criada
  (`ClassActivity`, construtor) e para cada atividade existente quando o aluno é matriculado depois
  (`AssignStudentToClassService`). Ou seja, o card já existe antes de qualquer entrega.
- `ClassActivityWorkStatus`: `Pending = 0`, `Delivered = 1`, `InReview = 2` (só prova vencida,
  calculado em `ClassActivity.GetWorkStatus`, nunca gravado), `Finalized = 3`.
- Entregas: `POST students/activities/{id}/works` e `POST students/activities/{id}/works/files`
  (URL pré-assinada, container `ClassActivityWorkFiles`, path
  `{institutionId}/{classId}/{activityId}/{studentId}/{ulid}.{ext}`; PNG/JPEG/WebP até 5 MB e PDF
  até 10 MB, num dicionário privado do `CreateClassActivityWorkFileService`). Os arquivos ficam em
  URL pública.
- Leituras que carregam o conteúdo da entrega:
  - `GET students/classes/{classId}/activities` (`GetStudentClassActivities`) devolve `WorkContent`
    de **todas** as atividades;
  - `GET students/classes/{classId}/activities/{activityId}` devolve `WorkContent`;
  - `GET teachers/classes/{classId}/activities/{activityId}` devolve `Works` com o `Content` de
    **todos** os alunos, em ordem alfabética, e `DeliveredWorks`, que já conta todo status diferente
    de `Pending`. O `DetailTeacher.vue` usa `work.content` para decidir entre "Sem entrega" e "Ver
    entrega".
- Nota: `PUT teachers/activities/{activityId}/works/{workId}/note` (`AddActivityNote`).
- Aproveitamento (`ClassGrade.Performance`) soma `Note × Weight` de **todas** as atividades,
  qualquer que seja o status. Uma entrega pendente já conta como 0. A nota também é lida em SQL cru
  (`caw.note`) em `GetClassService`, `GetStudentDetailsService` e `GetTeacherClassStudentsService`.
- Front:
  - `DetailStudent.vue` mostra "Minha entrega" com `WorkEditor.vue`: exibe a entrega com
    `MarkdownContent` e, com "Editar", abre o `RichEditor` com upload de imagem e PDF. Fica editável
    em qualquer status menos `Finalized`. Em prova, só o texto "Nota lançada pelo professor".
  - `DetailTeacher.vue` lista as entregas com "Ver entrega" (`WorkModal.vue`, só leitura) e
    "Dar nota"/"Editar nota" (`AddNoteModal.vue`).
  - `CreateWorkModal.vue` (link) já foi removido.
- Notificação: domain event → `IDomainEventHandler` → `ctx.AddCommand(...)` → handler cria
  `Notification` + `UserNotification`. O `UpdateClassActivity` é o exemplo mais recente.

## Conceito

O card é a linha do tempo de **uma** entrega (um aluno × uma atividade). Ele tem três tipos de item:

| Item | Quem cria | Conteúdo | Efeito no status |
|---|---|---|---|
| **Entrega** (versão) | aluno | markdown obrigatório | → `Delivered` |
| **Avaliação** | professor | nota e/ou feedback em markdown + decisão | → `Finalized` ou `ChangesRequested` |
| **Comentário** | ambos | markdown obrigatório | nenhum |

**Um item por ação, não um por campo alterado.** Uma avaliação é um gesto só do professor ("nota 6,
refaz porque falta a cardinalidade"). Quebrá-la em "nota mudou" + "status mudou" + "comentário"
poluiria a linha do tempo, triplicaria as notificações e deixaria sem lugar as regras que envolvem
a ação inteira (finalizar exige nota, pedir alterações exige feedback, a checagem de concorrência).
Toda mudança de status tem uma causa visível no card: a entrega ou a avaliação.

Para a linha do tempo mostrar a mudança sem recalcular, o item que muda o estado guarda o antes:
entrega e avaliação gravam `PreviousStatus`, e a avaliação grava `PreviousNote`. Assim a UI mostra
"Nota 0,0 → 7,0" e "reabriu a entrega", e o card serve de histórico auditável de notas ("tirou 6,
refez, tirou 8,5"), que hoje se perde a cada `AddNote`.

### Ciclo de vida

| De | Ação | Para | Condição |
|---|---|---|---|
| `Pending` | entrega | `Delivered` | |
| `Pending` | avaliação "finalizar" | `Finalized` | sem versão: é o zero por não entregar |
| `Delivered` | nova versão | `Delivered` | substitui a anterior como versão mais recente |
| `Delivered` | avaliação "finalizar" | `Finalized` | |
| `Delivered` | avaliação "pedir alterações" | `ChangesRequested` | |
| `ChangesRequested` | nova versão | `Delivered` | |
| `ChangesRequested` | avaliação "finalizar" | `Finalized` | |
| `ChangesRequested` | avaliação "pedir alterações" | `ChangesRequested` | novo feedback |
| `Finalized` | entrega | `Delivered` | **só se o card não tem nenhuma versão** (entrega atrasada depois do zero) |
| `Finalized` | avaliação "finalizar" | `Finalized` | reavaliação: a nota nova substitui a vigente |
| `Finalized` | avaliação "pedir alterações" | `ChangesRequested` | só com versão; reabre para reenvio |
| qualquer | comentário | — | |

- **Pedir alterações exige uma versão.** Sobre nada entregue, "pedir alterações" não diz nada que um
  comentário não diga ("cadê o trabalho?").
- **Entrega em `Finalized`.** Liberar sempre deixaria o aluno reenviar depois de um 8,5 para "tentar
  um 10" sem ser pedido. Bloquear sempre impediria a entrega atrasada de quem levou zero por não
  entregar. A regra fica no meio: o aluno entrega em `Finalized` só enquanto não houver nenhuma
  versão no card. Depois de uma versão avaliada, mudar alguma coisa passa pelo professor: o aluno
  comenta, e o professor reabre com "pedir alterações".
- Na entrega atrasada depois do zero, a nota vigente **não muda**. O zero continua contando no
  aproveitamento até o professor reavaliar, e o card volta a aparecer como "aguardando você" na
  listagem.
- Em prova, que não aceita entregas, a avaliação só pode finalizar.
- O comentário vale em qualquer status, de qualquer lado, inclusive em `Pending`: o professor pode
  cobrar a entrega antes de o aluno aparecer.

Status novo, com o próximo valor livre:

```csharp
[Description("Alterações solicitadas")]
ChangesRequested = 4,
```

### Cenários de referência

São os dois casos que o modelo de card precisa contar direito, e viram testes de integração.

**Professor fala primeiro.** O aluno não entregou. O professor comenta no card `Pending` ("lembra
que o prazo é sexta"). O aluno recebe a notificação, vê o comentário no card e responde ou entrega.

**Zero, entrega atrasada e nova nota.**

```
● Prof. João avaliou · 20/09 · Nota 0,0 · [Finalizada]
│ Não entregou no prazo.
● Maria entregou · versão 1 · 22/09 · [Entregue com atraso]        Finalizada → Entregue
● Maria comentou · 22/09
│ Professor, tive um problema de saúde, segue o atestado.
● Prof. João avaliou · 23/09 · Nota 0,0 → 7,0 · [Finalizada]
```

### A nota

A nota vigente continua em `ClassActivityWork.Note`, porque é dela que o aproveitamento e as
listagens leem. Cada avaliação grava na própria linha a nota que deu e a que substituiu.

`Note` passa a ser **`decimal?`**. Com default 0, um card `Pending` mostraria "Nota 0,0" no
cabeçalho, como se o professor já tivesse dado zero, e "o professor deu zero" (explícito, com motivo
na linha do tempo) seria indistinguível de "ninguém avaliou ainda". Nulo é "sem nota". O
aproveitamento trata nulo como 0, então a regra de hoje, em que o pendente conta zero, não muda.

- "Finalizar" exige nota.
- "Pedir alterações" aceita nota opcional: é a nota provisória, útil quando o professor quer dizer
  "vale 6 do jeito que está, refaz que sobe". Sem nota, a vigente não muda.
- Como o aproveitamento conta tudo, qualquer que seja o status, a nota provisória conta até ser
  substituída. A alternativa (ignorar `ChangesRequested` no cálculo) está em "Pontos em aberto".

### Atraso

`Deliver` não checa o prazo hoje, e este plano não muda isso: reenviar a pedido do professor depois
do prazo, ou entregar atrasado depois de um zero, tem que ser possível. O atraso vira
**informação**, calculado na leitura: a versão cujo `CreatedAt` é depois de `DueDate` + `DueHour`
recebe `IsLate = true`, e a UI mostra "Entregue com atraso". Não precisa de coluna.

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
    public decimal? PreviousNote { get; set; }
    public ClassActivityWorkReviewDecision? Decision { get; set; }
    public ClassActivityWorkStatus? PreviousStatus { get; set; }
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

**Uma tabela só**, e não uma por tipo. O card é lido sempre inteiro e em ordem cronológica, e os
três tipos têm as mesmas colunas de autoria, conteúdo e datas. Tabelas separadas obrigariam a um
`UNION` com ordenação em toda leitura, e cada feature futura (menção, reação, "lido") teria que
apontar para três lugares.

**Colunas tipadas, não `payload jsonb`.** Enquanto os tipos forem poucos e conhecidos, colunas nulas
com `CHECK` por tipo mantêm a validação no banco:

- `Submission`: `content` e `previous_status` preenchidos; `note`, `previous_note` e `decision`
  nulos;
- `Review`: `decision` e `previous_status` preenchidos; `note`, `previous_note` e `content`
  opcionais;
- `Comment`: só `content` (nulo apenas depois de excluído).

**Autor como `UserId`**, não `StudentId`/`TeacherId`, pelo motivo do `RichEditor.md`: o card
mistura os papéis, e a menção aponta para pessoa. O papel exibido ("Professor"/"Aluno") sai do
`EstudUser.Type`.

`DomainEntity` porque é da criação de um item que nascem as notificações (ver "Notificações").

`ClassActivityWorkEntryConfig`:

- índice `(class_activity_work_id, created_at)`, que é a leitura do card;
- `content` com `varchar(10000)`, o mesmo limite do plano de aula e da entrega;
- `note` e `previous_note` com a mesma precisão da nota do work;
- FK para `users` com `Restrict`: usuário com histórico não se apaga.

### Mudanças em `ClassActivityWork`

```csharp
public decimal? Note { get; set; }
public List<ClassActivityWorkEntry> Entries { get; set; } = [];
public DateTime? LastEntryAt { get; set; }
```

- **`Content` sai.** A última versão é a última `Submission` do card. Manter uma cópia no work seria
  uma segunda fonte da verdade, que um dia alguém atualiza de um lado e esquece do outro. As
  listagens que hoje carregam o conteúdo de todas as entregas não deveriam carregá-lo (ver
  "Leituras que mudam"). A única tela de um item só que precisa da última versão é o card, que já
  lê as entries.
- **`Note` vira `decimal?`**, nulo enquanto ninguém avaliou.
- `LastEntryAt` é desnormalizado para a lista do professor ordenar por "atividade recente" e mostrar
  "nova mensagem" sem subconsulta por linha.

`Status` e `Note` continuam no work: são estado derivado das entries, mas o aproveitamento e as
listagens leem direto deles, e só os métodos de domínio abaixo os alteram.

```csharp
public OneOf<ClassActivityWorkEntry, EstudError> Deliver(int authorUserId, string content)
public OneOf<ClassActivityWorkEntry, EstudError> Review(int authorUserId, decimal? note, string? feedback,
    ClassActivityWorkReviewDecision decision, int? lastSeenSubmissionId, bool acceptsWorks)
public ClassActivityWorkEntry Comment(int authorUserId, string content)
```

- `Deliver` recusa `Finalized` quando o card já tem alguma `Submission`
  (`ClassActivityWorkAlreadyFinalized`). Grava `PreviousStatus`.
- `Review` recusa finalizar sem nota, recusa pedir alterações sem feedback, sem versão ou em prova.
  Grava `PreviousStatus` e `PreviousNote`.
- `AddNote` sai, substituído por `Review`.

### Concorrência

O aluno pode mandar uma versão nova enquanto o professor está escrevendo a avaliação da anterior. A
avaliação carrega o `lastSeenSubmissionId`, que é a versão que o professor tinha na tela (nulo se o
card não tinha versão). Se existir uma mais nova, a avaliação é recusada com
`ClassActivityWorkHasNewSubmission`, e a UI recarrega o card sem perder o texto digitado. Isso cobre
também a corrida do zero: o professor zera um card `Pending` sem saber que o aluno acabou de
entregar, e a avaliação volta recusada.

### Migration

O repositório não versiona migrations do EF (o banco de teste sai do `EnsureCreatedAsync`), e a troca
`Link` → `Content` também entrou sem migration. Os passos abaixo viram um script SQL aplicado em
produção junto com o deploy da fase 1, **na ordem**, porque o passo 4 não tem volta.

1. Cria `class_activity_work_entries` e a coluna `last_entry_at`.
2. Backfill: toda entrega com `content` não nulo ganha uma entry `Submission` com o conteúdo, autor =
   `students.user_id`, `previous_status = Pending` e `created_at` = data da migration (não existe
   data de entrega hoje). `last_entry_at` recebe o mesmo valor.
3. `note` vira nullable, e recebe `null` onde `status = Pending`. Hoje `AddNote` sempre finaliza,
   então todo pendente tem nota 0 por default, não por avaliação.
4. Confere a contagem (works com `content` = entries `Submission`) e remove a coluna `content`.

**Nota antiga não vira entry `Review`**, porque não se sabe quem deu. O cabeçalho do card mostra a
nota vigente a partir de `ClassActivityWork.Note`, então nada se perde na tela.

## Backend

A convenção do projeto é endpoint por papel (`teachers/...`, `students/...`), e ela é mantida.
O `RichEditor.md` propunha rotas únicas para os dois papéis, mas a checagem de acesso fica mais
simples e óbvia com um endpoint por papel, e as policies continuam por `UserType`. O que é igual nos
dois lados fica no domínio e num mapper compartilhado.

### Features novas e alteradas

| Feature | Rota | Papel | O que faz |
|---|---|---|---|
| `GetTeacherClassActivityWork` | `GET teachers/activities/{activityId}/works/{workId}` | professor | card completo + navegação anterior/próximo |
| `ReviewClassActivityWork` | `POST teachers/activities/{activityId}/works/{workId}/reviews` | professor | nota + feedback + decisão |
| `ReviewPendingClassActivityWorks` | `POST teachers/activities/{activityId}/works/pending/reviews` | professor | zero em lote para os cards sem versão |
| `CreateTeacherWorkComment` | `POST teachers/activities/{activityId}/works/{workId}/comments` | professor | comentário |
| `CreateTeacherWorkFile` | `POST teachers/activities/{activityId}/works/{workId}/files` | professor | URL pré-assinada para imagem/PDF no feedback |
| `GetStudentClassActivityWork` | `GET students/activities/{activityId}/work` | aluno | o próprio card |
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
- **Acesso do aluno**: dono do card (`ClassActivityWork.StudentId`), matriculado na turma.
- **Editar e excluir** só o próprio **comentário**. Versão entregue e avaliação não se editam nem se
  apagam: uma é o que foi entregue, a outra é registro de nota. Corrigir uma avaliação é fazer outra.
  As duas features de autor ficam em `Back/Features/Cross/`, com `AddEstudPolicy(Nome)` (basta estar
  logado) e checagem `AuthorUserId == ctx.RequestUser.Id` no service.
- Comentário excluído some do card como "Comentário removido", mantendo a posição. O conteúdo é
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
até 10000 caracteres. As regras de combinação (finalizar exige nota, pedir alteração exige feedback
e versão) ficam no domínio, porque dependem do tipo de atividade e do card.

### Zero em lote

Com o zero por falta de entrega virando uma avaliação explícita, o professor vai querer aplicá-lo à
turma de uma vez. `ReviewPendingClassActivityWorks` recebe `Note` (default 0) e um `Feedback`
opcional ("Não entregou no prazo."), e cria uma `Review` "finalizar" em cada card da atividade que
ainda não tem nenhuma versão e não está `Finalized`. Cada card ganha o próprio item, com a mesma
notificação de uma avaliação individual. Só vale depois do prazo, para não zerar quem ainda pode
entregar.

### Saída do card

As duas rotas `GET` devolvem o mesmo formato, montado por um mapper compartilhado:

```csharp
public class ClassActivityWorkThreadOut
{
    public int WorkId { get; set; }
    public ClassActivityWorkStatus Status { get; set; }
    public decimal? Note { get; set; }
    public bool CanDeliver { get; set; }
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
    public decimal? PreviousNote { get; set; }
    public ClassActivityWorkReviewDecision? Decision { get; set; }
    public ClassActivityWorkStatus? PreviousStatus { get; set; }
    public int? Version { get; set; }
    public bool IsLate { get; set; }
    public bool CanEdit { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}
```

- `Version` (1, 2, 3...) é calculado na leitura, contando as entregas em ordem.
- `CanEdit` e `CanDeliver` vêm prontos do backend. O front não reimplementa as regras de autor e de
  entrega em `Finalized`.
- `PreviousWorkId`/`NextWorkId` só no professor, na ordem alfabética dos alunos, a mesma da lista.
  É o que permite corrigir a turma inteira sem voltar à listagem.

### Leituras que mudam

- `GetStudentClassActivities` e `GetStudentClassActivity`: `WorkContent` sai. A seção do aluno lê o
  card por `GetStudentClassActivityWork`.
- `GetTeacherClassActivity`: `Content` sai de cada item de `Works`, que ganha `HasSubmission` (o que
  o `DetailTeacher.vue` usava `content` para decidir), `IsLate` da última versão, `EntriesCount`,
  `LastEntryAt` e `AwaitingTeacher`. `AwaitingTeacher` é verdadeiro quando o status é `Delivered`
  **ou** quando o último item do card é do aluno, ou seja, quando o aluno falou depois da última fala
  do professor. É o "tem coisa para você aqui" da listagem, e dispensa controle de lido por enquanto.
  A entrega atrasada depois do zero cai aqui, porque volta o status para `Delivered`.
  `DeliveredWorks` passa a contar cards com versão, e não mais status diferente de `Pending`: um
  zero por falta de entrega é `Finalized` sem ter sido entregue.
- Nota nullable: `Value` e `PonderedValue` dos DTOs viram `decimal?`, `ClassGrade` usa `Note ?? 0`, e
  o SQL cru de `GetClassService`, `GetStudentDetailsService` e `GetTeacherClassStudentsService` passa
  a ler `coalesce(caw.note, 0)`.

### Erros novos

Em `Back/Errors/EstudErrors.Classes.cs`: `ClassActivityWorkAlreadyFinalized`,
`ClassActivityWorkHasNoSubmission` (pedir alterações sem versão),
`ClassActivityWorkHasNewSubmission`, `InvalidClassActivityWorkReview` (combinação de decisão, nota e
feedback), `InvalidClassActivityWorkComment`, `ClassActivityWorkEntryNotFound`,
`ClassActivityWorkEntryNotEditable` e `ClassActivityNotPastDue` (zero em lote antes do prazo).

### Policies

`Policies.Teachers.cs`: `GetTeacherClassActivityWork`, `ReviewClassActivityWork`,
`ReviewPendingClassActivityWorks`, `CreateTeacherWorkComment`, `CreateTeacherWorkFile`, todas com
`UserType.Teacher`. `Policies.Students.cs`: `GetStudentClassActivityWork` e
`CreateStudentWorkComment`, com `UserType.Student`. `Policies.Cross.cs`: `UpdateWorkComment` e
`DeleteWorkComment`, só logado. `AddActivityNote` sai.

## Notificações

Regras simples, sem inscrição, que o `Mentions.md` depois substitui pela versão com `Reason` e
`NotificationSubscription`:

| Evento | Quem recebe | Texto |
|---|---|---|
| Avaliação finalizando | aluno | "Sua entrega de *X* recebeu nota 8,5" |
| Avaliação pedindo alterações | aluno | "O professor pediu alterações em *X*" |
| Comentário do professor | aluno | "Novo comentário em *X*" |
| Nova versão depois de `ChangesRequested` | autor da última avaliação | "Maria enviou uma nova versão de *X*" |
| Entrega atrasada depois de `Finalized` | autor da última avaliação | "Maria entregou *X* com atraso" |
| Comentário do aluno | professores que já falaram no card; se nenhum, todos da turma | "Maria comentou em *X*" |

A primeira entrega dentro do prazo **não** notifica o professor: com 40 alunos, seriam 40
notificações por atividade. A lista com `AwaitingTeacher` resolve esse caso. A entrega atrasada
depois do zero notifica porque o professor já considerou o card encerrado e não vai voltar nele
sozinho.

Fluxo: criar a entry dispara `ClassActivityWorkEntryCreatedDomainEvent(Uid)`, cujo handler enfileira
`CreateWorkEntryNotificationCommand(entryId)`. O command decide destinatários e texto pelo tipo e
pelo `PreviousStatus`. Ele também **checa o acesso de novo**, porque o professor pode ter sido
desvinculado da turma entre o evento e o processamento. O zero em lote gera um command por card.

`NotificationType`, alinhado com a numeração reservada no `Mentions.md`:

```csharp
WorkCommentMention = 200,
WorkComment = 201,
WorkReviewed = 202,
WorkResubmitted = 203,
WorkDeliveredLate = 204,
```

Os links apontam para a tela do professor (`/classes/{c}/activities/{a}/works/{w}`) ou do aluno
(`/classes/{c}/activities/{a}`). Por isso, quando os destinatários têm papéis diferentes, o command
gera uma `Notification` por papel.

## Frontend

### Componentes

| Componente | Papel |
|---|---|
| `activities/WorkThread.vue` | recebe o card e renderiza a linha do tempo; usado pelos dois lados |
| `activities/WorkThreadEntry.vue` | um item: entrega, avaliação ou comentário |
| `activities/WorkComposer.vue` | editor + ações, com os modos que o papel e o status permitem |
| `activities/WorkReviewForm.vue` | nota + feedback + as duas ações do professor |
| `activities/NoteInput.vue` | a máscara de nota que hoje está no `AddNoteModal` |
| `pages/classes/[classId]/activities/[activityId]/works/[workId].vue` | tela de correção do professor |

`RichEditor.vue` ganha a prop `compact`, que troca o `min-h-64` por uma altura menor e cresce com o
texto. Menção continua com `:mention="false"` até a fase de menção.

`WorkEditor.vue` vira a base do `WorkComposer` (edição, upload, limite de caracteres, botão
desabilitado com upload em andamento). `WorkModal.vue` e `AddNoteModal.vue` saem: a leitura da
entrega e a nota passam a acontecer dentro do card, sem modal.

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
  (primário). Um select de decisão esconderia a consequência. Em card sem versão, só "Finalizar",
  com o texto de apoio "O aluno ainda poderá entregar com atraso".
- Com status `Finalized`, a aba "Avaliar" mostra "Reavaliar", e o texto de apoio explica que a nota
  nova substitui a atual.
- Avaliação que muda nota mostra "Nota 0,0 → 7,0". Entrega que tira o card de `Finalized` mostra
  "Finalizada → Entregue" ao lado do selo de atraso.
- Cabeçalho com `Note` nulo mostra "Sem nota", nunca "0,0".
- **Anterior/Próximo** preservam o fluxo de corrigir a turma em sequência. Depois de finalizar, um
  toast oferece "Ir para o próximo".
- Na listagem (`DetailTeacher.vue`), cada linha vira link para a tela de correção e mostra a
  contagem de mensagens e um ponto "aguardando você" (`AwaitingTeacher`). Filtros em `UTabs`: Todas
  · Aguardando · Alterações solicitadas · Finalizadas · Sem entrega. Depois do prazo, a aba "Sem
  entrega" ganha a ação "Dar zero para quem não entregou" (`ReviewPendingClassActivityWorks`), com
  confirmação e o feedback opcional.

### Tela do aluno

A seção "Minha entrega" do `DetailStudent.vue` passa a ser o card, na mesma página. Para o aluno só
existe um card por atividade, então não vale uma rota nova. **O card aparece sempre**, em qualquer
status: o professor pode ter comentado antes da entrega, e o comentário não pode ficar invisível.

- `Pending`: a linha do tempo (vazia ou com os comentários do professor) e o composer com
  "Entregar" (primário) e "Comentar".
- `Delivered`: "Comentar" e "Enviar nova versão".
- `ChangesRequested`: um `UAlert` no topo, "O professor pediu alterações", com link para a
  avaliação. O composer oferece "Enviar nova versão" (primário) e "Comentar".
- `Finalized` sem versão (o zero por não entregar): a nota em destaque, e o composer com "Entregar
  com atraso" e "Comentar". O texto de apoio avisa que a nota só muda se o professor reavaliar.
- `Finalized` com versão: só "Comentar". A nota aparece em destaque no cabeçalho da seção.
- Em prova, não existe entrega: só a avaliação do professor e os comentários.

Qual modo aparece vem do `CanDeliver` da API, não de regra no front.

### Comportamentos comuns

- **Rascunho**: o texto do composer é salvo em `localStorage` por `workId` + modo, e limpo ao
  enviar. Feedback longo não pode sumir por um F5.
- `Ctrl/⌘ + Enter` envia. Enviar fica desabilitado com upload em andamento (`v-model:uploading` já
  existe no `RichEditor`) ou com conteúdo vazio.
- Depois de enviar, `refresh()` do card e rolagem até o item novo. Não há atualização otimista: a
  ordem e a versão vêm do servidor.
- `ClassActivityWorkHasNewSubmission`: toast "O aluno enviou uma nova versão", `refresh()` do card e
  o texto digitado fica no composer.
- Comentário próprio tem menu (`UDropdownMenu`) com Editar/Excluir. Editar troca o item pelo editor
  no mesmo lugar. Excluir pede confirmação.
- Rótulo de papel ao lado do nome ("Professor") e datas relativas com `title` absoluto.
- Status novo em `Web/app/utils/classes.ts`: `ChangesRequested: 'Alterações solicitadas'`, cor
  `warning`. `InReview` passa a usar outra cor para não se confundir com ele.
- `workContent` sai dos tipos em `Web/app/types/classes.ts`, e `value` passa a aceitar `null`.

### Custo de render

Cada item exibido com `MarkdownContent` é uma instância de Tiptap. Um card de 30 itens são 30
editores. Mitigações, na ordem:

1. versões antigas recolhidas não montam o editor;
2. comentários curtos e sem formatação (a maioria) podem sair num `<p>` simples, e o
   `MarkdownContent` fica para quando houver marcação;
3. se ainda pesar, uma renderização de markdown sem editor (`@nuxtjs/mdc`) para os itens sem
   menção. É o que o `RichEditor.md` já prevê.

## Testes

Integração, um arquivo por feature, com as regions na ordem do CLAUDE.md e o cenário montado pelos
endpoints. Helpers novos no `TestsHttpClient` (`.Teachers` e `.Students`):
`GetTeacherClassActivityWork`, `ReviewClassActivityWork`, `ReviewPendingClassActivityWorks`,
`CreateTeacherWorkComment`, `CreateTeacherWorkFile`, `GetStudentClassActivityWork`,
`CreateStudentWorkComment`, `UpdateWorkComment` e `DeleteWorkComment`. Os testes de
`AddActivityNote` migram para `ReviewClassActivityWork`, e quem usava `AddActivityNote` como arrange
passa a usar o helper novo.

Cenários que importam:

- **Ciclo completo**: entrega → pedir alterações com nota 6 → aluno comenta → professor comenta →
  nova versão → finalizar com 8,5. O card tem 6 itens na ordem, versões 1 e 2, nota vigente 8,5,
  status `Finalized`, e a última avaliação tem `PreviousNote = 6`.
- **Professor fala primeiro**: professor comenta num card `Pending`. O aluno lê o comentário em
  `GetStudentClassActivityWork`, recebe `WorkComment`, e o status continua `Pending`.
- **Zero, entrega atrasada e nova nota**: professor finaliza um card sem versão com nota 0 → aluno
  entrega depois do prazo → aluno comenta → professor finaliza com 7. O card tem 4 itens; a entrega
  tem `IsLate = true` e `PreviousStatus = Finalized`; entre a entrega e a reavaliação a nota vigente
  continua 0 e o status é `Delivered`; a última avaliação tem `PreviousNote = 0`; o professor
  recebe `WorkDeliveredLate`.
- Nova versão em `Finalized` com versão → `ClassActivityWorkAlreadyFinalized`. Depois de "pedir
  alterações" sobre uma finalizada, a nova versão é aceita.
- Avaliar com `LastSeenSubmissionId` antigo, ou nulo num card que já tem versão →
  `ClassActivityWorkHasNewSubmission`.
- Finalizar sem nota, pedir alterações sem feedback, pedir alterações em prova →
  `InvalidClassActivityWorkReview`. Pedir alterações num card sem versão →
  `ClassActivityWorkHasNoSubmission`.
- Pedir alterações sem nota mantém a nota vigente. Com nota, atualiza a nota e o aproveitamento em
  `GetStudentClassActivities`.
- Card `Pending` volta com `Note = null`, e o aproveitamento continua contando como 0.
- Zero em lote: antes do prazo → `ClassActivityNotPastDue`. Depois, só os cards sem versão e não
  finalizados ganham avaliação; cards entregues e já avaliados ficam intactos.
- Professor de outra turma não lê, não avalia e não comenta (`TeacherNotAssignedToClass`). Aluno não
  lê o card de colega (a rota do aluno não recebe `workId`, então o teste é que ele só vê o
  próprio).
- Editar e excluir comentário de outra pessoa → `ClassActivityWorkEntryNotEditable`. Editar entrega
  ou avaliação pelo endpoint de comentário → mesmo erro. Comentário excluído volta com
  `Deleted = true` e `Content = null`.
- `GetTeacherClassActivity`: `AwaitingTeacher` fica verdadeiro depois do comentário do aluno e
  falso depois da resposta do professor. `HasSubmission` e `DeliveredWorks` não contam o zero por
  falta de entrega.
- Notificações, com `AwaitCommandsProcessing`: o aluno recebe `WorkReviewed` e `WorkComment`; quem
  avaliou recebe `WorkResubmitted` e `WorkDeliveredLate`; comentário do aluno sem professor no card
  notifica todos da turma; a primeira entrega no prazo não notifica ninguém; o autor nunca recebe a
  notificação do próprio item.
- Unidade (`ClassActivityWork`): a tabela de transições do ciclo de vida, `PreviousStatus` e
  `PreviousNote`, e as regras de `Review`.

## Faseamento

Cada fase vai para produção sozinha.

1. **Card com histórico de versões.** Entidade, `Note` nullable, migration com backfill e remoção
   do `Content`, `Deliver` criando `Submission` com a regra de `Finalized`,
   `GetStudentClassActivityWork`/`GetTeacherClassActivityWork` só leitura, e as leituras que mudam
   (`WorkContent` e `Content` saindo, `HasSubmission`, `coalesce` no SQL cru). No front, o card
   sempre visível para o aluno, mostrando as versões, e no lugar do `WorkModal` do professor.
2. **Avaliação com feedback.** `ReviewClassActivityWork` (inclusive sem versão), status
   `ChangesRequested`, remoção do `AddActivityNote`, tela de correção com Anterior/Próximo,
   `CreateTeacherWorkFile`. Isso já resolve "nota com explicação", "refaz" e o zero seguido de
   entrega atrasada.
3. **Comentários.** Criar, editar e excluir nos dois lados, `AwaitingTeacher` e filtros na
   listagem. É aqui que o professor passa a poder falar primeiro.
4. **Notificações simples**, pela tabela acima.
5. **Zero em lote** (`ReviewPendingClassActivityWorks`). Depende da 2 e aproveita as notificações da
   4.
6. **Menções**, que é a fase 4 do `RichEditor.md`, e depois o `Mentions.md` (inscrição, motivo,
   caixa agrupada, e-mail).

## Impacto nos outros planos

- `RichEditor.md` e `Mentions.md` falam em `ClassActivityWorkComment` e
  `ClassActivityWorkCommentMention`. Com a tabela única, os nomes passam a ser
  `ClassActivityWorkEntry` e `ClassActivityWorkEntryMention`. Um ganho é que menção passa a valer
  também no feedback da avaliação ("@Maria @João, o grupo todo refaz a parte 2").
- O `NotificationSubjectType.ClassActivityWork` do `Mentions.md` casa com este card: o assunto é o
  `workId`, e abrir a tela de correção ou a seção do aluno marca como lido por assunto.
- O item do TODO "notificar aluno quando o professor adicionar nota ou comentar, mesmo sem menção" é
  a fase 4 daqui.

## Evolução do card

O modelo "um item por ação" acomoda ações novas como tipos novos de entry, sem mexer nos existentes.
Os candidatos que já aparecem no horizonte:

- `Reopened`: o professor reabre o card para reenvio sem dar nota nem feedback. Hoje isso é "pedir
  alterações", que exige feedback.
- `DeadlineExtended`: o "reenviar até dd/mm" dos pontos em aberto.
- `NoteChanged`: nota alterada **por fora do card** (lançamento em lote na tela de notas, ajuste da
  coordenação, importação). Sem ele, o histórico de notas do card mente. Só entra quando existir
  uma dessas origens; enquanto a única forma de mudar nota for a avaliação, ele é redundante.

A regra para criar um tipo novo: ele representa uma ação de alguém que não é nem entrega, nem
avaliação, nem comentário. Um tipo que só descreve um campo que mudou (`StatusChanged`) não entra,
porque toda mudança de status tem que ter uma ação por trás.

## Pontos em aberto

- **Nota provisória no aproveitamento.** O plano conta a nota de `ChangesRequested` porque o cálculo
  atual já conta tudo. A alternativa é o aproveitamento ignorar entregas em `ChangesRequested`, para
  o aluno não ver um aproveitamento "punido" por algo que ele ainda vai refazer. Decisão pedagógica.
- **Entrega atrasada depois do zero sempre aceita?** O plano aceita sempre que o card não tem
  versão. Alguns professores podem querer fechar de vez ("não aceito atrasado"). Seria uma opção na
  atividade (`AcceptsLateWorks`) ou na avaliação, e fica para quando alguém pedir.
- **Pedir alterações exige feedback?** O plano exige. Se incomodar ("refaz, você sabe o porquê"),
  basta afrouxar a regra no domínio ou criar o `Reopened`.
- **Prazo para reenvio.** Hoje é indefinido. Pode fazer sentido o professor informar "reenviar até
  dd/mm" na avaliação. Seria o `DeadlineExtended` de "Evolução do card".
- **Edição com janela de tempo.** O plano deixa editar o próprio comentário sempre, marcando
  "editado". Limitar a, digamos, 15 minutos evita reescrever a conversa depois da resposta do outro
  lado.
- **Gestor lendo cards.** Coordenação vendo feedback dos professores é um pedido provável. Não entra
  agora; seria um terceiro `GET` com policy de gestor.
- **Webhook** para `WorkReviewed`: o `ClassActivityWorkEntryCreatedDomainEvent` já é domain event,
  então entra de graça se o sistema de webhooks listar eventos de domínio.

## Fora do escopo

- Entrega em grupo (ver `RichEditor.md`).
- Reações, respostas aninhadas, citação de trecho da entrega ("comentário inline" na linha X).
- Rubrica (critérios com pontuação parcial).
- Comparação visual entre versões (diff).
- Tempo real no card. Por ora é `refresh()` ao enviar, mais a notificação.
