# Editor rico (UEditor)

O `UEditor` do Nuxt UI entra no Estud por dois caminhos que compartilham a mesma base técnica e as
mesmas decisões de formato:

1. **Planejamento de aula** — o professor escreve texto formatado, o aluno lê. Já detalhado na seção
   2 do `Plans/LessonPlanning.md`; aqui ficam só os complementos que surgiram da leitura do código do
   componente.
2. **Comentários com menção na entrega de atividade** — professor e alunos conversam sobre uma
   entrega, mencionando pessoas com `@`. Nada disso existe hoje; é o grosso deste plano.

A ordem importa: o uso 1 é o que estabelece formato de armazenamento, sanitização e componente de
exibição. O uso 2 herda essas decisões e acrescenta menções, notificação e um domínio novo.

## O componente

Nenhuma dependência nova. O `@nuxt/ui` instalado é a **4.8.1** (o `Web/package.json` pede `^4.7.1`) e
traz Tiptap 3.24 e ProseMirror como dependências diretas.

| Componente | Serve para |
|---|---|
| `UEditor` | o editor em si, sobre Tiptap 3 |
| `UEditorToolbar` | barra de ações — fixa, bubble ou floating |
| `UEditorMentionMenu` | menu de sugestão disparado por um caractere (`@` por padrão) |
| `UEditorSuggestionMenu` | menu de barra (`/`) |
| `UEditorEmojiMenu` | menu de emoji (`:`) |
| `UEditorDragHandle` | reordenar blocos |

Fatos do código, não da documentação (`node_modules/@nuxt/ui/dist/runtime/components/Editor.vue`):

- As extensões `Image` e `Mention` vêm **ligadas por padrão** (props `image` e `mention`, default
  `true`) — desligar é explícito, com `:image="false"` / `:mention="false"`.
- `contentType` decide o que o `v-model` recebe: `getHTML()`, `getJSON()` ou `getMarkdown()`, com
  fallback para `getText()` se a serialização lançar (`Editor.vue:160-171`).
- `starterKit: false` derruba toda a formatação e deixa só parágrafo, texto e histórico.

Setup obrigatório: incluir os pacotes `prosemirror-*` em `vite.optimizeDeps.include`
(`Web/nuxt.config.ts`), senão o dev server carrega duas instâncias do ProseMirror. Já feito.

## Formato de armazenamento

A decisão do `LessonPlanning.md` é **markdown puro na coluna**, e ela se sustenta também para os
comentários. Dois achados de código reforçam:

- **Documento vazio em markdown vira string vazia.** O serializador tem um `isEmptyOutput` que
  limpa `&nbsp;` e espaço não-quebrável e devolve `""`
  (`@tiptap/markdown/dist/index.js:339-348`). Em HTML o mesmo documento sairia como `<p></p>`, e o
  `ClassLesson.UpdatePlan` (`Back/Domain/Classes/ClassLesson.cs:39`), que usa `IsEmpty()`, gravaria
  lixo em vez de `null`. O mesmo vale para a checagem de comentário vazio.
- **Menção sobrevive ao round-trip em markdown.** O `@tiptap/extension-mention` registra um
  `createInlineMarkdownSpec` (`dist/index.js:177`) que serializa o nó como um shortcode
  self-closing:

  ```
  [@ id="12" label="Zaqueu do Vale"]
  ```

  e registra o tokenizer que faz o caminho de volta. Ou seja: não é preciso JSON nem HTML para
  preservar a identidade de quem foi mencionado.

O contraponto é que **isso não é markdown padrão**. O `<MDC>` do `@nuxtjs/mdc` não conhece esse
shortcode e vai renderizá-lo como texto literal. Daí a divisão:

| Conteúdo | Guardar | Exibir |
|---|---|---|
| Planejamento de aula (sem menção) | markdown | `<MDC :value="..." />` |
| Comentário de entrega (com menção) | markdown | `UEditor` com `:editable="false"` |

Usar o próprio `UEditor` em modo leitura é o que garante que o shortcode volte a virar um nó de
menção com a classe `.mention`, e evita escrever um parser paralelo no front.

## Uso 1 — planejamento de aula

O plano está no `Plans/LessonPlanning.md`, seção 2. Implementado em
`Web/app/components/lessons/PlanEditor.vue`:

- `:mention="false"` e `:image="false"`. Menção não faz sentido num plano de aula, e imagem depende
  da seção 3 daquele plano (upload), que ainda não existe — deixar a extensão ligada dá ao professor
  um botão que só aceita URL externa.
- `UEditorSuggestionMenu` e `UEditorEmojiMenu` desligados — nenhum dos dois é montado
  automaticamente, então basta não declarar.
- O limite de 2000 caracteres do `UpdateLessonPlanService.cs` passou a contar marcação, e subiu para
  10000 nos dois lados.
- O `handler` de `link` é sobrescrito: o padrão do Nuxt UI abre um `prompt('Enter the URL:')` em
  inglês.
- O editor mora numa rota própria (`/classes/{classId}/lessons/{lessonId}`), carregada com
  `LazyLessonsDetailTeacher` — Tiptap + ProseMirror não entram no chunk da tela de turma.

## Uso 2 — comentários com menção na entrega de atividade

### Onde isso encosta hoje

O fluxo de entrega é mínimo:

- `ClassActivity` (`Back/Domain/Classes/ClassActivity.cs`) cria um `ClassActivityWork` por aluno da
  turma no construtor.
- `ClassActivityWork` (`Back/Domain/Classes/ClassActivityWork.cs`) guarda `Link`, `Note` e `Status`.
  `AddLink` marca `Delivered`, `AddNote` marca `Finalized`.
- O aluno entrega com `POST students/activities/{id}/works`
  (`Back/Features/Students/CreateClassActivityWork/`), o professor dá nota com
  `Back/Features/Teachers/AddActivityNote/`.
- Front: `Web/app/components/activities/DetailTeacher.vue` (lista de entregas + `AddNoteModal`) e
  `DetailStudent.vue` (a entrega do próprio aluno + `CreateWorkModal`).

Não existe comentário, não existe feedback textual, e **não existe grupo**: a entrega é sempre
um-para-um entre aluno e atividade. A entrega em grupo é o cenário que motiva a menção, mas é uma
feature própria — ver "Entrega em grupo" mais abaixo.

### Domínio

Entidade nova, em `Back/Domain/Classes/`:

```csharp
public class ClassActivityWorkComment
{
    public int Id { get; set; }
    public int ClassActivityWorkId { get; set; }
    public int AuthorUserId { get; set; }
    public string Content { get; set; }          // markdown
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<ClassActivityWorkCommentMention> Mentions { get; set; }
}
```

E a tabela de menções resolvidas:

```csharp
public class ClassActivityWorkCommentMention
{
    public int CommentId { get; set; }
    public int UserId { get; set; }
}
```

Por que persistir a menção separada, se ela já está no texto: é ela que dispara notificação, que
responde "fui mencionado onde?" e que permite marcar como lida. Reparsear markdown a cada consulta
para descobrir isso é caro e frágil.

**Quem preenche essa tabela é o backend**, parseando o `Content` recebido. O front até sabe quem foi
mencionado, mas aceitar uma lista vinda do cliente permite que texto e lista divirjam — e o texto é
a fonte de verdade, porque é ele que o leitor vê. O parse é um regex sobre
`\[@\s+([^\]]*)\]` com leitura dos atributos `id` e `label`.

Autor como `UserId` e não `StudentId`/`TeacherId`: o mesmo thread mistura os dois papéis, e a
menção aponta para uma pessoa, não para uma matrícula.

### Segurança

O `label` viaja dentro do markdown, escrito pelo cliente. Três consequências:

1. **Nunca confiar no `label`.** Na exibição, resolver o nome pelo `id` (ou aceitar que o label é
   só um cache de exibição e pode estar velho se a pessoa mudou de nome). Guardar o label junto é o
   que mantém o texto legível mesmo se o usuário for removido.
2. **Validar o `id` contra a turma.** Um aluno pode escrever `[@ id="999" label="Diretor"]` na mão.
   O backend descarta toda menção cujo `UserId` não seja de alguém vinculado à turma da atividade —
   caso contrário vira canal de notificação para qualquer usuário da instituição, e pior, entre
   instituições.
3. **Menção descartada não silencia o comentário.** O texto é salvo como veio; só não gera
   notificação nem linha na tabela de menções. Alternativa mais rígida — rejeitar o comentário
   inteiro com `InvalidCommentMention` — também é defensável; a primeira é menos irritante.

Fora isso valem as mesmas travas do planejamento: HTML cru e sintaxe `::componente` do MDC
desligados na renderização, e nenhum sanitizador existe no backend hoje (o `Back.csproj` não tem
Markdig nem AntiXss).

### Endpoints

Seguindo a estrutura vertical de `Back/Features/`:

| Feature | Rota | Quem |
|---|---|---|
| `CreateWorkComment` | `POST activities/{activityId}/works/{workId}/comments` | professor e aluno |
| `GetWorkComments` | `GET activities/{activityId}/works/{workId}/comments` | professor e aluno |
| `DeleteWorkComment` | `DELETE .../comments/{commentId}` | autor, ou professor da turma |
| `GetClassMentionables` | `GET classes/{classId}/mentionables` | professor e aluno |

As checagens de acesso são as que `AddActivityNoteService` e `CreateClassActivityWorkService` já
fazem, só que agora precisam valer para os dois papéis no mesmo endpoint: professor vinculado à
turma (`ClassTeachers`) **ou** aluno dono da entrega (`ClassActivityWork.StudentId`). Vale extrair
isso para um helper no `EstudDbContext`, ao lado de `GetTeacherId` / `GetStudentId`.

`GetClassMentionables` é o que alimenta o menu: professores da turma e alunos matriculados, com
`Id` (user id), `Name` e `ProfilePhoto` (`EstudUser.ProfilePhoto` já existe). Endpoint novo porque
o `GetTeacherClassStudents` é policy de professor e devolve nota e frequência — dado que aluno
nenhum pode ver sobre colega.

Policies novas em `Back/Auth/Policies/Policies.Classes.cs`, no padrão do
`Policies.Notifications.cs`: as que não exigem permissão especial são só `AddEstudPolicy(Nome)`.

### Notificação

O sistema já resolve: `Notification` + `UserNotification` por usuário, criados dentro de um
`ICommandHandler`, exatamente como o `CreateNewClassActivityNotificationCommand`
(`Back/Features/Teachers/CreateClassActivity/`).

- `CreateWorkComment` grava o comentário e as menções e enfileira um
  `CreateWorkMentionNotificationCommand(commentId)` via `ctx.AddCommand(...)`.
- O handler monta uma `Notification.WorkCommentMention(...)` com link para
  `/classes/{classId}/activities/{activityId}` e cria um `UserNotification` por mencionado,
  **exceto o próprio autor**.
- Novo valor em `NotificationType`.

Vale um segundo tipo de notificação, sem menção, para "sua entrega recebeu um comentário" — é o
caso mais comum do professor dando feedback. Decidir se entra nesta etapa ou depois.

### Front

No `activities/DetailTeacher.vue` e `activities/DetailStudent.vue`, uma seção de thread abaixo da
entrega. Componente compartilhado, digamos `activities/WorkComments.vue`, porque os dois lados
mostram a mesma coisa:

```vue
<UEditor v-model="content" content-type="markdown" :image="false">
  <template #default="{ editor }">
    <UEditorToolbar :editor="editor" />
    <UEditorMentionMenu
      :editor="editor"
      :items="mentionables"
      ignore-filter
      v-model:search-term="searchTerm"
    />
  </template>
</UEditor>
```

- `ignore-filter` + `v-model:search-term` com `refDebounced` (`@vueuse/core` já está no projeto) é o
  caminho para busca no servidor. Para turma de 40 alunos, carregar a lista inteira uma vez e deixar
  o filtro interno do componente trabalhar é mais simples — e é o que eu faria primeiro.
- Os itens aceitam `label`, `avatar`, `icon`, `description`, `disabled`. Usar `description` para
  distinguir papel ("Professor" / "Aluno"), que é o que evita mencionar a pessoa errada.
- Exibição de cada comentário com `UEditor :editable="false"`, pelo motivo da seção de formato.
- Estilo da menção: o Nuxt UI aplica `class: "mention"` no `span`; o CSS é por conta do projeto
  (`Web/app/assets/css/main.css`).
- Lazy: o thread só monta quando a seção aparece.

### Entrega em grupo

É o cenário que o usuário descreveu, e é **feature separada** — hoje `ClassActivity.New` cria um
`Work` por aluno e não há nada que os agrupe. O caminho seria:

- `ClassActivityGroup` (id, activityId, nome) e `ClassActivityWork.GroupId` nullable, ou um
  `ClassActivityWork` compartilhado com uma tabela de membros.
- A nota passa a ser do grupo, com a opção de nota individual — decisão pedagógica, não técnica.
- O thread de comentários já nasce pronto para isso: ele pendura no `Work`, e o `Work` é que muda de
  dono. Os "mencionáveis" passam a incluir naturalmente os colegas de grupo.

O comentário com menção é útil **antes** dos grupos existirem: professor pede alteração e marca o
aluno, aluno responde. Não vale bloquear um no outro.

## Faseamento

1. **Exibição markdown no planejamento** (`<MDC>`), mantendo o `UTextarea`. Sem backend, sem risco.
2. **`UEditor` no planejamento**, com menção e imagem desligadas. Aqui é onde o editor é exercitado
   de verdade pela primeira vez.
3. **Comentários sem menção** na entrega: entidade, `CreateWorkComment`, `GetWorkComments`, thread no
   front. Entrega valor sozinho.
4. **Menção**: `GetClassMentionables`, `UEditorMentionMenu`, tabela de menções, validação contra a
   turma, notificação.
5. **Entrega em grupo**, se e quando.

## Riscos e pontos em aberto

- **Nada do `UEditor` foi exercitado em runtime neste projeto.** A serialização de menção foi lida no
  código do pacote, não observada. Vale um espetinho numa página de teste antes de fechar o formato
  — em especial o round-trip markdown → editor → markdown com menção no meio de texto formatado.
- **Shortcode de menção é formato de Tiptap, não padrão.** Se um dia o Estud trocar de editor, os
  comentários já gravados precisam de migração. O par (texto, tabela de menções) reduz o dano: os
  ids não se perdem.
- **Peso do bundle.** Tiptap + ProseMirror não é leve, e agora aparece em duas telas. Lazy nas duas.
- **Edição de comentário** com menções: editar o texto tem que re-sincronizar a tabela de menções, e
  decidir se menção nova gera notificação (sim) e se remover menção apaga a notificação (não).
- A decisão de formato vale também para descrição de atividade (`ClassActivity.Description`, hoje
  texto plano) e para o corpo das notificações. Escolher uma vez resolve as quatro.

## Fora do escopo

- Upload de imagem — `Plans/LessonPlanning.md`, seção 3.
- Reações, resposta aninhada (thread dentro de thread), edição colaborativa.
- Menção de outros tipos com `#` (atividade, aula), que o `UEditorMentionMenu` suporta via `char` +
  `plugin-key` distintos.
- Notificação por e-mail da menção — hoje a notificação é só in-app.
