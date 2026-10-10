# Menções de usuários

Plano para ligar `@menção` nos textos em markdown do Estud, começando pelos comentários da linha do
tempo da entrega (`ClassActivityWorkEntry`).

Relação com os planos existentes:

- **`Plans/RichEditor.md`** previa guardar `[@ id="12" label="..."]` no banco, com o `label` como
  cache de exibição, e uma tabela `ClassActivityWorkCommentMention`. Este plano **substitui essa
  parte**: o banco guarda só o id, e a tabela de menções muda de forma (ver "Domínio"). O comentário
  virou `ClassActivityWorkEntry` (`Type = Comment`) desde então.
- **`Plans/Mentions.md`** cuida do que acontece depois da menção salva: assunto, motivo, inscrição,
  caixa agrupada, e-mail. Este plano entrega só a notificação mínima ("fulano mencionou você") e
  deixa os ganchos para aquele plano evoluir.

## Decisões

| Tema | Decisão |
|---|---|
| Formato no banco | shortcode do Tiptap **só com id**: `[@ id="42"]` |
| Nome exibido | resolvido pelo backend em toda leitura, a partir do `EstudUser.Name` atual |
| Onde o nome entra | o backend devolve o markdown já com `label`: `[@ id="42" label="Maria Silva"]` |
| Front | não muda o formato: recebe e envia o shortcode completo, o Tiptap faz o resto |
| Índice de menções | tabela `mentions`, mantida no create/update, com uma FK nullable por tipo de conteúdo |
| Id inválido | erro `InvalidMention`, o conteúdo não é salvo |
| Usuário excluído | `label="Usuário removido"` |
| Avatar | só no menu de sugestão; dentro do texto a menção é `@Nome` |

Por que só o id: o nome vive num lugar só. Troca de nome (nome social, correção de cadastro) vale na
hora em todo texto, e anonimizar um usuário (LGPD) tira o nome dele de todo conteúdo de terceiros sem
reescrever nada. É o modelo do Basecamp (anexo com id assinado, resolvido ao renderizar), do Slack e
do Discord (`<@id>`). O GitHub guarda `@login` em texto e é o contraexemplo: renomear quebra as
menções antigas.

Por que o backend devolve o markdown já resolvido, e não um mapa `id → nome`: o front fica igual ao
de hoje, sem `NodeView` nem lookup, e a mesma rotina serve para notificação e e-mail. O custo é que só
dá para mostrar o nome no texto. Se um dia quisermos avatar ou cartão ao passar o mouse, passamos a
devolver também um mapa. O formato gravado é o mesmo, então a troca não exige migração de dados.

## Os três formatos do mesmo texto

| Onde | Texto |
|---|---|
| Banco | `Ajuste o diagrama, [@ id="42"].` |
| Resposta da API | `Ajuste o diagrama, [@ id="42" label="Maria Silva"].` |
| Notificação / e-mail | `Ajuste o diagrama, @Maria Silva.` |

## Backend

### `MentionsService`

Serviço único em `Back/Mentions/`, registrado como `IEstudService`. Toda feature que grava ou devolve
markdown com menção passa por ele.

```csharp
public class MentionsService(EstudDbContext ctx) : IEstudService
{
    public NormalizedMarkdown Normalize(string? markdown);
    public Task<OneOf<EstudSuccess, EstudError>> Validate(IEnumerable<int> userIds, MentionScope scope);
    public Task<Dictionary<string, string>> Resolve(IEnumerable<string?> markdowns);
    public Task<string> ToPlainText(string? markdown);
}

public record NormalizedMarkdown(string? Markdown, HashSet<int> UserIds);
```

- **`Normalize`**: parse dos shortcodes `[@ ...]`, descarta qualquer atributo além de `id` e devolve o
  texto só com `[@ id="N"]` e o conjunto de ids. Não confia no `label` vindo do cliente.
- **`Validate`**: uma query (`WHERE id IN (...)`) confere que todos os ids são mencionáveis no
  contexto (`MentionScope`, ver abaixo). Qualquer id fora → `InvalidMention.I`.
- **`Resolve`**: recebe **vários** textos, junta os ids de todos, busca os nomes numa query só e
  reescreve cada shortcode com `label`. Uma listagem de 30 comentários faz uma query, não 30.
- **`ToPlainText`**: troca o shortcode por `@Nome`, para notificação, e-mail e prévias.

Regras do parser:

- Regex sobre `\[@\s+([^\]]*)\]`, lendo os atributos com `(\w+)="([^"]*)"`, o mesmo formato do
  `createInlineMarkdownSpec` do Tiptap.
- **Ignorar código:** antes de procurar shortcodes, separar blocos cercados (```` ``` ````) e código
  inline (`` ` ``). A substituição só acontece nos trechos de fora. Sem isso, um exemplo escrito
  dentro de um bloco de código vira menção.
- **Escapar o nome no `Resolve`:** o tokenizer do Tiptap para no primeiro `]` e o atributo é
  delimitado por `"`. Nome com `"` ou `]` precisa de escape, ou quebra o shortcode na volta.
- `id` não numérico ou ausente → o shortcode é tratado como menção inválida (`InvalidMention`).

### Quem pode ser mencionado

`MentionScope` diz em que contexto a menção acontece. Para o primeiro consumidor:

```csharp
public record MentionScope(int ClassId);
```

Mencionáveis numa entrega da turma `ClassId`: professores em `ClassTeachers` e alunos em
`ClassStudents` daquela turma, sempre dentro de `ctx.RequestUser.InstitutionId`. A regra vira um
método só no `MentionsService`, usado pelo `Validate` e pelo endpoint de mencionáveis.

Sem essa validação, um aluno escreve `[@ id="999"]` na mão e passa a notificar qualquer usuário da
instituição.

### Domínio

```csharp
public class Mention
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int MentionedUserId { get; set; }
    public EstudUser? MentionedUser { get; set; }
    public int AuthorUserId { get; set; }
    public EstudUser? Author { get; set; }
    public int? ClassActivityWorkEntryId { get; set; }
    public ClassActivityWorkEntry? ClassActivityWorkEntry { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- Tabela `estud.mentions`, em `Back/Domain/Mentions/` com o `MentionConfig`.
- **Uma FK nullable por tipo de conteúdo**, e não `source_type` + `source_id`: com FK de verdade o
  banco garante integridade, apaga em cascata quando o conteúdo some e o EF usa navigation property,
  como o CLAUDE.md pede. Tipo de conteúdo novo = coluna nova.
- `CHECK` garantindo exatamente uma FK de conteúdo preenchida.
- Índice único `(class_activity_work_entry_id, mentioned_user_id)`.
- Índice `(mentioned_user_id, created_at)` para "minhas menções".
- O texto continua sendo a fonte de verdade. A tabela é um índice derivado: se divergir, dá para
  reconstruí-la relendo os textos.

### Create

Hoje há dois caminhos que gravam comentário:
`Back/Features/Students/CreateClassActivityWorkComment/` e
`Back/Features/Teachers/CreateClassActivityWorkEntry/`. Nos dois:

1. `Normalize(data.Content)`.
2. `Validate(ids, new MentionScope(classId))`. Se falhar, devolve o erro e não salva.
3. Grava o `ClassActivityWorkEntry` com o texto normalizado.
4. Cria um `Mention` por id, **exceto o próprio autor**.
5. `ctx.AddCommand(new NotifyMentionsCommand(mentionIds))`.
6. Tudo num `SaveChangesAsync` só: não pode existir menção sem texto nem texto sem menção.

### Update

Comentário de entrega ainda não é editável. Quando passar a ser (e para os próximos consumidores):

1. `Normalize` + `Validate`, como no create.
2. Ids antigos (da tabela) × ids novos (do texto).
3. Insere `novos − antigos`, apaga `antigos − novos`.
4. **Notifica só `novos − antigos`.** Sem isso, cada edição notifica de novo todo mundo já mencionado.
5. Menção removida não apaga a notificação já entregue.

O texto chega do front com `label` (veio do GET) e volta a ser normalizado. O que vai pro banco nunca
depende do nome que o cliente mandou.

### Get

`GetTeacherClassActivity` e `GetStudentClassActivity` devolvem as entries da linha do tempo. No
service, antes do mapper:

```csharp
var resolved = await mentions.Resolve(entries.Select(x => x.Content));
```

e o mapper usa `resolved[entry.Content]` no `Content`. Mesma regra para qualquer endpoint futuro que
devolva markdown com menção.

Esquecer o `Resolve` num endpoint faz o front mostrar `@42`: sem `label`, o Tiptap exibe o id. É um
erro que aparece na hora. Com o `label` gravado, o esquecimento passaria despercebido, mostrando o
nome antigo.

### Mencionáveis

| Feature | Rota | Quem |
|---|---|---|
| `GetClassMentionables` | `GET classes/{classId}/mentionables` | professor e aluno da turma |

Devolve `{ userId, name, profilePhoto, role }` dos professores e alunos da turma. É um endpoint novo
porque o `GetTeacherClassStudents` é exclusivo de professor e devolve nota e frequência, dados que um
aluno não pode ver sobre colegas. A policy nova vai em `Back/Auth/Policies/Policies.Classes.cs`.

Para uma turma de até ~50 pessoas, devolve todo mundo de uma vez e o filtro fica no front. Busca no
servidor (`?q=`) só se aparecer turma grande.

### Notificação

`NotifyMentionsCommand(List<int> MentionIds)`, no padrão do `CreateNewClassActivityNotificationCommand`:

- Carrega as menções com autor, entry, entrega, atividade e turma.
- **Rechecha acesso:** entre o comentário e o processamento, a pessoa pode ter saído da turma.
- Cria `Notification.WorkCommentMention(...)` com link para `/classes/{classId}/activities/{activityId}`
  e uma prévia do texto via `ToPlainText`, e um `UserNotification` por mencionado.
- `NotificationType.WorkCommentMention` com o próximo valor livre do enum.

Assunto, motivo, inscrição, agrupamento e e-mail ficam no `Plans/Mentions.md`.

### Erros

`InvalidMention` em `Back/Errors/EstudInvalidErrors.cs`: "Menção inválida: a pessoa mencionada não
participa desta turma." Entra no `ErrorExamplesProvider<...>` dos dois controllers de comentário.

## Front

### Editor

No `Web/app/components/RichEditor.vue`, uma prop `mentionables?: Mentionable[]`:

- `:mention="!!mentionables"` no lugar do `:mention="false"`.
- Dentro do slot, ao lado da toolbar e do `UEditorSuggestionMenu`:

  ```vue
  <UEditorMentionMenu v-if="mentionables" :editor="editor" :items="mentionItems" />
  ```

- `mentionItems` mapeia cada pessoa para `{ id: userId, label: name, avatar: { src: profilePhoto, alt: name }, description: 'Professor' | 'Aluno' }`.
  O `description` com o papel evita mencionar a pessoa errada quando há nomes parecidos.
- Ver no código do componente se o `id` passa intacto do item para o atributo do nó. O
  `EditorMentionMenu.vue` faz `attrs: { ...item }`, então deve passar.
- Botão `kind: 'mention'` na toolbar (handler pronto do Nuxt UI) quando houver `mentionables`.

`WorkComposer.vue` e `WorkModal.vue` buscam o `GetClassMentionables` e passam a lista. Composable
`useClassMentionables(classId)` com `useFetch` e cache por turma.

### Exibição

- `Web/app/components/MarkdownContent.vue`: trocar `:mention="false"` por `:mention="true"`. Sem
  isso, o shortcode aparece cru na tela de leitura.
- CSS da classe `.mention` em `Web/app/assets/css/main.css`: cor primária, peso médio, fundo leve.
- Em qualquer lugar que mostre o markdown fora do editor (prévias, `WorkTimeline.vue` se exibir
  trecho), usar texto que já venha do `ToPlainText` do backend.

## Testes

Integração, um arquivo por feature, regions na ordem do CLAUDE.md, cenário montado via endpoints.

`GetClassMentionables`:
- 401 sem login; 403 para usuário de outra turma.
- Devolve professores e alunos da turma, e só eles.

Comentário com menção (aluno e professor):
- Mencionar alguém de fora da turma → `InvalidMention`, nada salvo.
- Shortcode com `id` não numérico → `InvalidMention`.
- Menção válida: GET devolve `label` com o nome atual; o banco (assert via `GetDbContext`, porque a API
  não expõe o texto cru) guarda `[@ id="N"]` sem `label`.
- `label` falso enviado pelo cliente (`[@ id="N" label="Diretor"]`) é descartado: GET devolve o nome
  real.
- Trocar o nome do usuário mencionado → GET seguinte já devolve o nome novo.
- Shortcode dentro de bloco de código não vira menção nem é validado.
- Mencionar a si mesmo não gera `Mention` nem notificação.
- Mencionado recebe notificação após `AwaitCommandsProcessing()`; o autor não.

Unitários do `MentionsService` (parser): normalização, escape de `"` e `]`, código ignorado, vários
shortcodes no mesmo parágrafo, shortcode malformado.

## Faseamento

1. **`MentionsService`** (parser, `Normalize`, `Resolve`, `ToPlainText`) com testes unitários. Não
   muda comportamento nenhum ainda.
2. **Resolve nos GETs** das entries. Sem menções no banco ainda, é só passar o texto pela função.
3. **Escrita:** tabela `mentions`, `Validate`, normalização nos dois endpoints de comentário,
   `InvalidMention`.
4. **Front:** `GetClassMentionables`, `UEditorMentionMenu` no `RichEditor`, `mention` ligado no
   `MarkdownContent`, CSS.
5. **Notificação:** `NotifyMentionsCommand`.
6. **"Minhas menções"** (opcional): endpoint sobre a tabela `mentions`, filtrando pelo acesso atual
   ao conteúdo.

Depois disso, a evolução segue o `Plans/Mentions.md`.

## Riscos e pontos em aberto

- **Round-trip não testado em runtime.** Antes da fase 3, confirmar num editor de teste que markdown
  → editor → markdown preserva `[@ id="N" label="..."]` no meio de texto formatado (negrito, lista,
  link) e que um shortcode sem `label` carrega sem erro.
- **Erro vs. descarte** no id inválido: o `Plans/RichEditor.md` preferia descartar a menção e salvar o
  texto. Este plano escolhe erro, porque descartar esconde bug e deixa no texto um shortcode que o GET
  vai mostrar como "Usuário removido". Revisar se a experiência ficar ruim: por exemplo, aluno que saiu
  da turma enquanto o colega digitava.
- **Busca full-text:** o texto no banco não tem nomes, então buscar pelo nome de quem foi mencionado
  não funciona pelo `to_tsvector` do `Plans/FullTextSearch.md`. Se precisar, buscar pela tabela
  `mentions`.
- **Cobertura dos GETs:** cada endpoint novo que devolve markdown precisa chamar o `Resolve`. Vale um
  teste de integração por endpoint que confira o `label`.
- **SQL ad hoc e suporte** passam a ver só ids no texto.

## Fora do escopo

- Avatar ou cartão da pessoa dentro do texto (exigiria mapa + `NodeView`).
- Menção de grupo (`@turma`, `@professores`).
- Menção em descrição de atividade e plano de aula. A infraestrutura aceita: cada lugar novo é uma
  coluna FK em `mentions`, um `MentionScope` e o `Resolve` no GET correspondente.
- Menção de outras entidades com `#` (atividade, aula).
