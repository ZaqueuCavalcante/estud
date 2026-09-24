# Menções e notificações por assunto

Como o Estud passa de "avisos em massa" para o modelo de GitHub/Basecamp: menção que chega na
pessoa certa, inscrição em assunto, agrupamento na caixa, e-mail atrasado que se cancela sozinho.

O `Plans/RichEditor.md` já decide **como a menção nasce e é guardada** (editor, shortcode
`[@ id="12" label="..."]` em markdown, tabela `ClassActivityWorkCommentMention`, validação contra a
turma). Este plano cuida do que acontece **depois que o comentário é salvo**: quem recebe, por quê,
como aparece e por qual canal. O primeiro consumidor é o comentário na entrega de atividade
(`ClassActivityWork`), mas a infraestrutura é genérica.

## Como os outros fazem

| | GitHub | Basecamp |
|---|---|---|
| Menção | parse de `@login` no markdown, no servidor | nó de anexo com id assinado no HTML (`<bc-attachment sgid>`) |
| Quem acompanha | inscrição por thread, com **motivo** (`author`, `mention`, `comment`, `manual`...) | lista de *subscribers* por item; "X pessoas serão notificadas" antes de postar |
| Caixa | 1 linha por (usuário, thread); evento novo reabre a mesma linha | menu "Hey!" agrupado por item |
| E-mail | enviado se a notificação não foi lida | enviado com atraso, respeita horário de trabalho |
| Sair | "Unsubscribe" por thread; persistido para não reinscrever | "Unsubscribe" por item |

Os três conceitos que o Estud não tem hoje: **motivo**, **inscrição** e **assunto**.

## O que já existe

- `Notification` (`Back/Domain/Notifications/Notification.cs`): conteúdo compartilhado, com
  `Metadata` em JSON carregando os `links`.
- `UserNotification`: PK `(UserId, NotificationId)`, só `ViewedAt`.
- Fan-out na escrita, dentro de `ICommandHandler`: o
  `CreateNewClassActivityNotificationCommand` cria uma `Notification` e um `UserNotification` por
  aluno.
- Front: `useNotifications.ts` faz polling do `unread-count` a cada 60s;
  `NotificationsSlideover.vue` lista; `MarkNotificationsAsViewed` marca uma ou todas.
- `EmailsService` sabe mandar e-mail por template (Brevo), mas nenhuma notificação vai por e-mail.
- `Command` tem `delaySeconds` → `NotBefore`, que é o que o e-mail atrasado precisa.

O modelo compartilhado (`Notification` 1 : N `UserNotification`) **fica**. Ele é o certo para aviso
em massa, e também serve para os eventos novos: o texto "Novo comentário em X" é o mesmo para todos
os inscritos. O que varia por pessoa é o **motivo**, e isso vai para a `UserNotification`.

## Domínio

### Assunto

O assunto é "a coisa sobre a qual se conversa": é nele que a pessoa se inscreve, é por ele que a caixa
agrupa, e abrir a tela dele marca tudo como lido.

```csharp
public enum NotificationSubjectType
{
    ClassActivityWork = 0,
}
```

Na `Notification`, nullable, porque aviso em massa e boas-vindas não têm assunto:

```csharp
public NotificationSubjectType? SubjectType { get; set; }
public int? SubjectId { get; set; }
public int? ActorUserId { get; set; }
```

`ActorUserId` é quem causou o evento. Ele serve para o avatar na lista e para excluir o autor dos
destinatários.

Índice `(subject_type, subject_id)` na `notifications`.

### Motivo

```csharp
public enum NotificationReason
{
    Broadcast = 0,
    Owner = 1,
    Subscribed = 2,
    Comment = 3,
    Mention = 4,
}
```

Na `UserNotification`:

```csharp
public NotificationReason Reason { get; set; }
public DateTime? EmailedAt { get; set; }
```

Linhas existentes migram com `Reason = Broadcast`.

**Precedência** quando a mesma pessoa cai em mais de um grupo no mesmo evento:
`Mention > Owner > Comment > Subscribed`. Ela recebe **uma** notificação, com o motivo mais forte.
Os valores do enum não expressam essa ordem; a ordem mora num método, para não amarrar numeração
persistida à regra de negócio:

```csharp
public static int Priority(this NotificationReason reason) => reason switch
{
    NotificationReason.Mention => 4,
    NotificationReason.Owner => 3,
    NotificationReason.Comment => 2,
    NotificationReason.Subscribed => 1,
    _ => 0,
};
```

### Inscrição

Entidade nova, em `Back/Domain/Notifications/`:

```csharp
public class NotificationSubscription
{
    public int UserId { get; set; }
    public NotificationSubjectType SubjectType { get; set; }
    public int SubjectId { get; set; }
    public NotificationReason Reason { get; set; }
    public bool Subscribed { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- PK `(UserId, SubjectType, SubjectId)`.
- `Subscribed = false` registra que a pessoa **saiu**. Sem essa linha, o próximo comentário a
  reinscreveria como `Comment` e o "parar de acompanhar" não duraria.
- `Reason` guarda **por que ela entrou**. Esse valor alimenta o filtro "Participando" e o texto
  "você está recebendo isto porque...".

Regras de entrada, sempre *insert-if-not-exists* (nunca sobrescreve um `Subscribed = false`):

| Quem | Quando | Reason |
|---|---|---|
| Aluno dono da entrega | primeiro comentário na entrega | `Owner` |
| Autor do comentário | ao comentar | `Comment` |
| Mencionado válido | ao ser mencionado | `Mention` |
| Qualquer um com acesso | clicou "Acompanhar" | `Subscribed` |

A exceção é a menção. Ser mencionado **reativa** uma inscrição com `Subscribed = false`, porque é
alguém chamando a pessoa pelo nome, e esse é o comportamento do GitHub. Vale revisar se incomodar.

Os professores da turma **não** entram automaticamente em toda entrega: 40 alunos × N atividades é
ruído. O professor entra quando comenta, quando é mencionado ou quando clica em acompanhar.

## Pipeline

```
CreateWorkComment (request)
  ├─ valida acesso e conteúdo
  ├─ grava ClassActivityWorkComment + menções válidas     (RichEditor.md)
  ├─ garante inscrições: dono, autor, mencionados
  └─ ctx.AddCommand(NotifyWorkCommentCommand(commentId))
                     │
                     ▼
NotifyWorkCommentCommandHandler (Quartz)
  ├─ carrega comentário, entrega, atividade, turma
  ├─ candidatos = inscritos ativos ∪ mencionados do comentário
  ├─ remove o autor
  ├─ remove quem perdeu acesso à entrega
  ├─ um motivo por pessoa (precedência)
  ├─ uma Notification por texto distinto:
  │     "Maria mencionou você"    → Reason = Mention
  │     "Novo comentário em ..."  → demais motivos
  ├─ UserNotification por destinatário
  └─ ctx.AddCommand(SendNotificationsEmailCommand(userId), delaySeconds: 600)
```

Fazer isso no command, e não na request, segue o padrão do `CreateNewClassActivityNotificationCommand`.
Com isso, o fan-out não pesa no tempo de resposta e ganha retry de graça.

**Checar acesso no handler**, e não só ao inscrever: entre a inscrição e o evento, o aluno pode ter
saído da turma ou o professor pode ter sido desvinculado. A regra de acesso é a mesma do
`RichEditor.md`: professor em `ClassTeachers` da turma **ou** aluno dono da entrega. Ela deve ser um
helper único no `EstudDbContext`, usado pelo endpoint e pelo handler.

Factories novas em `Notification`, no padrão de `NewClassActivity`:

```csharp
public static Notification WorkCommentMention(int institutionId, int actorUserId, string actorName,
    int classId, int activityId, int workId, string activityTitle)

public static Notification WorkComment(int institutionId, int actorUserId, string actorName,
    int classId, int activityId, int workId, string activityTitle)
```

As duas setam `SubjectType = ClassActivityWork`, `SubjectId = workId` e o link em `Metadata`.
`NotificationType` ganha `WorkCommentMention = 200` e `WorkComment = 201`.

### Edição de comentário

- Re-sincronizar a tabela de menções: parse do texto novo, diff com as linhas atuais.
- **Menção nova** → inscreve e enfileira a notificação só para os novos (o command recebe a lista de
  user ids, ou um flag "apenas menções novas").
- **Menção removida** → apaga a linha de menção, **não** apaga a notificação já entregue, nem a
  inscrição.
- Edição sem menção nova não notifica ninguém.

### Comentário excluído

Soft delete (`DeletedAt`, já previsto no `RichEditor.md`). A notificação continua apontando para a
entrega, que continua existindo, então o link não quebra. O comentário aparece como "removido".

## Caixa de notificações

### Agrupamento por assunto, na leitura

Duas formas de agrupar:

1. **Na escrita (GitHub):** uma linha por (usuário, assunto), atualizada a cada evento.
2. **Na leitura:** uma linha por evento, agrupada na consulta.

A escolha é **2**. Com a PK atual `(UserId, NotificationId)`, "reaproveitar" a linha exigiria
apagar e reinserir. Além disso, perderia o histórico, que o `GetInstitutionNotification` e a tela
`/notifications/[id]` usam. Para o volume de uma turma, o custo da consulta é irrelevante.

`GetNotificationsService` passa a devolver um item por assunto (ou por notificação, se não tiver
assunto), com o evento mais recente e a contagem:

```sql
WITH rows AS (
    SELECT
        n.*, un.viewed_at, un.reason,
        COALESCE(n.subject_type::text || ':' || n.subject_id, 'n:' || n.id) AS group_key
    FROM estud.user_notifications un
    JOIN estud.notifications n ON n.id = un.notification_id
    WHERE un.user_id = @UserId
      {unreadFilter}
),
grouped AS (
    SELECT
        *,
        ROW_NUMBER() OVER (PARTITION BY group_key ORDER BY created_at DESC) AS rn,
        COUNT(*) FILTER (WHERE viewed_at IS NULL) OVER (PARTITION BY group_key) AS unread_in_group,
        MAX(reason) OVER (PARTITION BY group_key) AS top_reason
    FROM rows
)
SELECT ..., COUNT(*) OVER() AS total_rows
FROM grouped
WHERE rn = 1
ORDER BY created_at DESC
LIMIT @PageSize OFFSET @Offset
```

`MAX(reason)` só funciona se a ordem numérica do enum bater com a precedência, e ela não bate de
propósito. Por isso a consulta precisa de um `CASE reason WHEN 4 THEN 4 WHEN 1 THEN 3 ...`, ou então
de um `BOOL_OR(reason = 4) AS has_mention`, que é o que importa na tela: "você foi mencionado aqui".
Prefira o `BOOL_OR`.

`GetNotificationsItemOut` ganha `SubjectType`, `SubjectId`, `Reason`, `HasMention`, `UnreadCount` e o
ator (`ActorName`, `ActorPhoto`).

O `GetUnreadNotificationsCount` passa a contar **grupos** com não lidas, para bater com o que a lista
mostra. Senão, o badge diz 12 e a lista mostra 3 itens.

### Marcar como lido por assunto

É isso que faz o sistema parecer inteligente: abrir a entrega marca como lidas todas as notificações
dela, e é também o que cancela o e-mail pendente.

`MarkNotificationsAsViewedIn` ganha `SubjectType?` + `SubjectId?`. Os três modos (uma, todas, por
assunto) ficam no mesmo endpoint, com o validator exigindo exatamente um deles:

```csharp
var query = ctx.UserNotifications.Where(x => x.UserId == ctx.RequestUser.Id && x.ViewedAt == null);

if (data.SubjectType != null)
    query = query.Where(x => x.Notification!.SubjectType == data.SubjectType
        && x.Notification.SubjectId == data.SubjectId);
else if (!data.MarkAll)
    query = query.Where(x => x.NotificationId == data.NotificationId);
```

Trocar o `ToListAsync` + loop por `ExecuteUpdateAsync`: marcar tudo como lido para quem tem
centenas de notificações não precisa carregar nada.

### Acompanhar / parar de acompanhar

| Feature | Rota |
|---|---|
| `GetSubscription` | `GET notifications/subscriptions/{subjectType}/{subjectId}` |
| `UpdateSubscription` | `PUT notifications/subscriptions/{subjectType}/{subjectId}` com `{ subscribed }` |

O `GET` devolve `{ subscribed, reason }`, e a tela mostra "Você está acompanhando porque comentou". O
`PUT` checa acesso ao assunto. Sem isso, qualquer um se inscreveria em qualquer entrega e receberia
notificação dela.

O acesso ao assunto é resolvido por um `switch` no `SubjectType`, que chama o mesmo helper do
handler. Tipo de assunto novo = um `case` novo.

## E-mail

### O atraso que se cancela

`SendNotificationsEmailCommand(userId)`, enfileirado com `delaySeconds: 600` a cada notificação
criada. O handler:

1. Busca as `UserNotification` do usuário com `ViewedAt == null`, `EmailedAt == null` e
   `Reason != Broadcast`.
2. Se não houver nenhuma, encerra: a pessoa já leu no app, ou um command anterior já mandou.
3. Senão, manda **um** e-mail com todas, agrupadas por assunto, e seta `EmailedAt`.

Esse passo 2 dá deduplicação sem precisar de lock. Dez comentários em sequência enfileiram dez
commands: o primeiro que rodar manda um resumo com os dez, e os outros nove não encontram nada. Se
os comandos puderem rodar em paralelo, vale um `SELECT ... FOR UPDATE SKIP LOCKED` nas linhas, ou
setar `EmailedAt` com `ExecuteUpdateAsync ... RETURNING` antes de montar o e-mail.

`Broadcast` fica fora do e-mail por ora: um aviso para a instituição inteira viraria milhares de
e-mails, o que é outra conversa (custo do Brevo, opt-in).

### EmailsService

Hoje cada método tem um template com um único placeholder `{{link}}`. Um resumo precisa de lista.
Duas opções:

- **Montar o HTML dos itens em C#** e injetar num `{{items}}` do template. Simples, e combina com o
  que existe.
- Adotar um motor de template (Scriban, Fluid). É mais limpo, mas é dependência nova.

A primeira basta. Novo método `SendNotificationsDigestEmail(to, name, items)` e template
`NotificationsDigest.html`.

### Preferências

Coluna nova em `EstudUser`, sem tabela própria por enquanto:

```csharp
public enum EmailNotificationsPreference
{
    All = 0,
    MentionsOnly = 1,
    None = 2,
}
```

Default `MentionsOnly`: menção é o caso em que alguém quer **aquela** pessoa. "Novo comentário"
por e-mail é o que faz gente marcar o remetente como spam. O filtro entra no passo 1 do handler.
Tela em `/account`.

Preferência por tipo × canal (matriz estilo GitHub) fica para quando existir um segundo canal.

## Tempo real

O polling de 60s continua sendo a base. Para o badge reagir na hora, o caminho é **SSE**:
`GET notifications/stream` mantém a conexão aberta e manda `unread-changed` quando o usuário ganha
uma notificação. O front, ao receber, chama o `fetchUnreadCount` que já existe.

O problema é que o evento nasce no **processador de commands** e a conexão SSE está em **alguma**
instância da API. Com mais de uma instância no Railway, é preciso um backplane. O Postgres já resolve:

- O handler, depois de salvar, faz `NOTIFY estud_notifications, '<userId>'`.
- Cada instância da API tem um `BackgroundService` com `LISTEN estud_notifications` e repassa para
  as conexões SSE daquele usuário.

É a fase mais cara e a que menos muda a percepção, porque 60s de atraso numa caixa de notificação é
aceitável. Fica por último, e é opcional.

## Front

- **`NotificationsSlideover.vue`**: um item por grupo, com avatar do ator, "Maria e mais 2
  comentaram em *Trabalho 1*", destaque quando `hasMention`, e badge de `unreadCount` do grupo.
  Clicar navega pelo link do `metadata`. Não é preciso marcar como lido no clique, porque a tela de
  destino marca.
- **Tela da entrega** (`activities/DetailTeacher.vue` / `DetailStudent.vue`): ao montar o thread,
  `markAsViewed({ subjectType, subjectId })`. Botão "Acompanhar / Parar de acompanhar" com o motivo
  no tooltip. Se ele abrir modal, aplicar o `blur()` do CLAUDE.md.
- **Antes de enviar o comentário**: uma linha com "Serão notificados: Maria, João" (a ideia do
  Basecamp), calculada no front a partir de inscritos (vindos do `GetSubscription`, estendido com a
  lista) + menções do texto. É opcional, mas evita a surpresa de "quem vai ver isso?".
- `useNotifications.markAsViewed` passa a aceitar os três modos.

## Testes

Integração, arquivo por feature, regions na ordem do CLAUDE.md, cenário sempre via endpoints. Os
helpers novos no `TestsHttpClient` são `CreateWorkComment`, `GetNotifications`,
`MarkNotificationsAsViewed(subject)`, `UpdateSubscription` e `GetSubscription`.

Cenários que importam:

- Mencionado recebe notificação com `Reason = Mention`. O autor não recebe nada, nem se mencionar a
  si mesmo.
- Mencionado de fora da turma: comentário salvo, nenhuma notificação.
- Inscrito e mencionado no mesmo comentário: **uma** notificação, motivo `Mention`.
- Dono da entrega recebe o comentário do professor com `Reason = Owner`.
- Quem deu `UpdateSubscription(false)` não recebe o próximo comentário, e **recebe** se for mencionado.
- Aluno removido da turma depois de inscrito não recebe.
- Três comentários na mesma entrega viram um item em `GetNotifications`, com `UnreadCount = 3`. O
  `unread-count` conta 1.
- Marcar por assunto zera o grupo e não toca nos outros.
- Editar adicionando menção notifica só o novo. Editar removendo menção não apaga a notificação.
- `UpdateSubscription` num assunto sem acesso → erro.

## Faseamento

Pressupõe as fases 3 e 4 do `RichEditor.md` (comentário e menção gravados).

1. **Motivo e assunto no modelo.** Colunas em `Notification`/`UserNotification`, migration com
   `Broadcast` nas linhas antigas, `NotifyWorkCommentCommand` notificando só os mencionados. Isso já
   entrega "fui mencionado".
2. **Inscrições.** `NotificationSubscription`, regras de entrada, dono e autor notificados, endpoints
   de acompanhar.
3. **Caixa agrupada.** `GetNotifications` e `unread-count` por grupo, marcar por assunto, slideover
   novo.
4. **E-mail.** Command atrasado, template de resumo, preferência na conta.
5. **Tempo real (opcional).** SSE + `LISTEN/NOTIFY`.
6. **Limpeza.** Job Quartz apagando notificações lidas com mais de N dias (a tabela cresce linear com
   usuários × eventos).

## Riscos e pontos em aberto

- **Reativar inscrição por menção** é escolha de produto. O GitHub faz, e há quem odeie.
- **Grupo agregado na leitura** depende da consulta com janela. Se a caixa ficar lenta com muito
  histórico, o limpador da fase 6 resolve antes de mudar o modelo.
- **E-mail em paralelo**: a deduplicação por "não achei nada" assume um processamento por usuário
  por vez. Confirmar como o `CommandsProcessorJob` paraleliza antes da fase 4.
- **Menção de grupo** (`@turma`, `@professores`): fora do escopo. Quando entrar, é um fan-out
  grande.
- **Rate limit de menção**: alguém mencionando a mesma pessoa em dez comentários seguidos. O
  agrupamento na caixa e o resumo por e-mail já amortecem. Só tratar se virar problema.
- **Aviso em massa agora "sem assunto"**: continua funcionando igual, como um grupo de um item.
  Nada muda para `CreateNotification`.

## Fora do escopo

- Push mobile / web push.
- Responder por e-mail (reply-to que vira comentário).
- Horário de silêncio / "não perturbe".
- Menção em outros lugares além da entrega (aula, atividade). A infraestrutura aceita, e cada lugar
  novo é um `NotificationSubjectType` + regra de acesso + regras de inscrição.
