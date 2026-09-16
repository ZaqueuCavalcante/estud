# Planejamento de aula

Primeiro passo do diário de classe: o professor escreve o que vai ser abordado em cada aula, e o
aluno vê esse planejamento. O conteúdo ministrado (o registro com validade legal) e a retificação
ficam para depois.

## Base já implementada

O fluxo ponta a ponta está no repositório, com markdown:

- `PUT teachers/lessons/{lessonId}/plan` (`Back/Features/Teachers/UpdateLessonPlan/`),
  `GET students/classes/{classId}/lessons` (`Back/Features/Students/GetStudentClassLessons/`) e
  `GET teachers/classes/{classId}/lessons/{lessonId}`
  (`Back/Features/Teachers/GetTeacherClassLesson/`).
- `ClassLesson.PlannedContent` + `UpdatePlan()`, erro `InvalidClassLessonPlan`, policies novas,
  `PlannedContent` no out do `GetTeacherClassLessons`.
- Testes de integração das features e helpers no `TestsHttpClient`.
- Front: tela de detalhes da aula (`Web/app/components/lessons/DetailTeacher.vue`, rota
  `/classes/{classId}/lessons/{lessonId}`) com as seções Planejamento (`lessons/PlanEditor.vue`) e
  Chamada (`lessons/AttendanceEditor.vue`), cada uma com modo leitura e modo edição; a aba Aulas do
  `classes/DetailTeacher.vue` só lista as aulas e leva pra essa tela; a seção "Aulas" do
  `DetailStudent.vue` exibe o planejamento com `MarkdownContent.vue`.

Falta rodar `dotnet build` / `dotnet test` para validar.

## 1. Banco

O repositório não versiona migrations (não existe `ModelSnapshot`), e os testes criam o schema com
`EnsureCreatedAsync` (`Tests/Base/IntegrationTestBase.cs:56`). Nos testes a coluna aparece sem
nenhum passo extra. Para os ambientes, gerar a migration e o script conforme
`Back/Database/Migrations.md`; o SQL resultante é
`ALTER TABLE estud.class_lessons ADD planned_content text NULL;`.

## 2. Markdown no planejamento

Objetivo: o professor escrever texto rico (títulos, listas, negrito, links, código) e o aluno ver
o texto formatado, no mesmo espírito das issues do GitHub e dos cards do Basecamp.

### O que já existe no projeto

Nenhuma dependência nova é necessária:

| Peça | Origem | Serve para |
|---|---|---|
| `UEditor` / `UEditorToolbar` | Nuxt UI 4.7.1 | editor WYSIWYG, sobre Tiptap 3 |
| `@tiptap/markdown` | dependência do `@nuxt/ui`, já em `node_modules/.pnpm` | markdown ↔ documento |
| `<MDC>`, `<MDCRenderer>`, `parseMarkdown` | `@nuxtjs/mdc` 0.22, transitivo do `@nuxt/content` | renderizar markdown em runtime |
| `prose/*` (H1–H4, P, Ul, Table, Code, Blockquote, Callout…) | Nuxt UI | o visual que o `/docs` já tem |

O `/docs` usa `ContentRenderer` + `queryCollection('docs')`, que é build-time: coleções de arquivos
`.md` compiladas para SQLite. Isso não se aplica a texto escrito pelo usuário. O motor por baixo
(`@nuxtjs/mdc`) é que funciona com string solta, e é dele que sai o mesmo visual.

### Decisões

- Guardar **markdown puro** no banco. A coluna continua `planned_content text` — nenhuma mudança de
  schema além da migration da seção 1.
- Exibição: `<MDC :value="plannedContent" />`, que mapeia para os componentes `Prose*`.
- Edição: `UEditor` com `content-type="markdown"` (`EditorContentType = 'json' | 'html' | 'markdown'`),
  que entrega e recebe markdown como string.

### Passos

1. ~~Declarar `@nuxtjs/mdc` no `Web/package.json`.~~ Pendente: hoje ele só existe como dependência
   transitiva do `@nuxt/content`, e depender disso quebra em qualquer upgrade do content. O
   `pnpm install` precisa rodar do Windows — o `node_modules` do repositório tem bindings nativos de
   `win32`, e do WSL o pnpm quer apagar e reinstalar tudo.
2. ✅ `DetailStudent.vue`: o `<p whitespace-pre-line>` do planejamento virou `MarkdownContent.vue`.
3. ✅ A aba Aulas do `classes/DetailTeacher.vue` não mostra mais o planejamento — cada aula tem um
   botão que leva pra tela de detalhes, e o markdown renderizado vive lá.
4. ✅ `lessons/PlanEditor.vue`: `UEditor` + `UEditorToolbar` com `v-model` em markdown, direto na
   página no modo edição, no lugar do antigo `LessonPlanModal.vue`.
5. ✅ Exibição: o `MarkdownContent.vue` usa o próprio `UEditor` com `:editable="false"`. O `<MDC>`
   deixava o conteúdo em branco e o `parseMarkdown` quebrava no navegador (causa não investigada).
   O editor só gera nós do schema do Tiptap, então HTML cru e `::componente` não viram markup. Não
   existe nenhum sanitizador no backend hoje — o `Back.csproj` não tem Markdig, AntiXss nem equivalente.
6. ✅ Limite de tamanho: de 2000 para 10000, no `UpdateLessonPlanService.cs` e no editor.
7. ✅ Testes de integração: o `GetTeacherClassLesson` cobre o round-trip do markdown.

### Em aberto

- Nada disso foi exercitado em runtime — `pnpm typecheck`, `pnpm lint` e `pnpm dev` só rodam do
  Windows neste checkout.
- A mesma decisão vale para descrição de atividade e notificações. Escolher uma vez resolve as três.

## 3. Upload de imagem no planejamento

O professor arrasta ou cola uma imagem no editor, o Estud sobe para o storage e referencia a URL no
markdown. Depende da seção 2 — só faz sentido com texto rico.

### Situação atual

A interface já existe, a implementação é inteiramente greenfield:

- `Back/Storage/IStorageService.cs` tem um único método:
  `CreatePreSignedUrlForUpload(StorageContainer container, string path)`.
- Só existe o `FakeStorageService`, registrado **incondicionalmente** em
  `Back/Configs/ServicesConfigs.cs:19` — diferente de `IEmailsService` e `IGoogleService`, que são
  trocados por ambiente.
- `StorageContainer` (`Back/Domain/Enums/StorageContainer.cs`) tem um único valor, `ProfilePhotos`,
  e nenhuma feature o usa.
- Não há nenhum pacote de storage no `Back.csproj` — nem AWS, nem Azure. A nota em
  `Web/app/pages/dev/overview.vue:81` diz que "o pacote está no csproj"; está desatualizada e vale
  corrigir junto.

A assinatura do método já aponta para o modelo de **URL pré-assinada**, que é o caminho abaixo.

### Provedor

A proposta é S3, o `overview.vue` fala em Azure Blob. Como o `IStorageService` abstrai, isso é
escolha de pacote e settings, não de arquitetura. `AWSSDK.S3` atende S3 e qualquer compatível.

Vale considerar **Cloudflare R2**: é S3-compatible (mesmo SDK e mesmo código), não cobra egress, e o
Cloudflare já está na frente do domínio. A Railway não tem storage nativo.

### Fluxo

1. O professor arrasta ou cola a imagem no editor.
2. O front pede a URL: `POST teachers/lessons/{lessonId}/plan/images`, com `contentType` e
   `sizeInBytes`.
3. O backend valida (professor vinculado à turma, mime na allowlist, tamanho), monta o path e
   devolve `{ uploadUrl, publicUrl }`.
4. O front faz `PUT` direto no bucket, com a `uploadUrl`.
5. O front insere `![](publicUrl)` no markdown.
6. O plano é salvo pelo `PUT teachers/lessons/{id}/plan` de sempre — **o endpoint do plano não muda**.

O binário nunca passa pela API. A alternativa (upload via API com `IFormFile`) dá mais controle, mas
joga toda a banda e a memória no backend.

### Estrutura do path

Container novo no enum:

```csharp
[Description("lesson-plan-images")]
LessonPlanImages,
```

```
lesson-plan-images/{institutionId}/{classId}/{lessonId}/{ulid}.{ext}
```

- `institutionId` primeiro: permite política por tenant no bucket e apagar ou exportar uma
  instituição inteira por prefixo.
- `lessonId` agrupa a aula, então remover a aula vira remover um prefixo.
- O nome do arquivo **não pode ser sequencial**. Os ids do projeto são `int` incrementais; se o nome
  também for previsível, a URL de uma imagem de outra instituição é adivinhável. Usar
  `Ulid.NewUlid()`, como o `DomainEntity.Uid` (`Back/DomainEvents/DomainEntity.cs:5`).
- A extensão sai do content-type validado, nunca do nome do arquivo enviado pelo cliente.

### Quem pode ver a imagem — decidir antes de implementar

O path sozinho não autoriza ninguém. Três caminhos:

1. **Bucket público + nome aleatório.** A URL funciona direto no markdown e nunca expira; a proteção
   é ser inadivinhável. Quem tiver o link vê. É o que a maioria das ferramentas faz.
2. **Bucket privado + URL assinada de leitura.** Mais rígido, mas a URL expira — e markdown com link
   que morre obriga a reescrever as URLs a cada renderização.
3. **Servir pela API**, com checagem de permissão. Controle real, ao custo de banda no backend.

(1) é o default pragmático, (3) o rigoroso. O peso aqui é que a imagem pode conter dado sensível
(print de nota, enunciado de prova), o que empurra para (3).

### Ciclo de vida

Imagem enviada com o plano nunca salvo, ou removida do texto depois, vira lixo no bucket. Opções:

- Entidade `LessonPlanImage` (`Id`, `LessonId`, `Path`, `ContentType`, `SizeInBytes`, `CreatedAt`),
  reconciliada com o que o markdown referencia — no espírito do `EnrollmentProof`, que persiste o
  registro do artefato sem persistir o artefato.
- O sistema de Commands já resolve o agendamento: `NotBefore` permite "apagar se continuar sem
  referência em 24h".
- Ou uma lifecycle rule no bucket, ou simplesmente aceitar o lixo no começo.

O `IStorageService` vai precisar crescer: hoje só tem upload, faltaria `Delete`.

### Validações

- Allowlist de content-type: `image/png`, `image/jpeg`, `image/webp`.
- Tamanho máximo (ex.: 5 MB), validado na assinatura e reforçado no bucket
  (`content-length-range` na policy).
- Professor vinculado à turma da aula — a mesma checagem do `UpdateLessonPlanService`.
- Teto de imagens por aula, e rate limiter próprio no endpoint de assinatura.

### Front

- O `UEditor` já expõe `onPaste` e `onDrop`, e a extensão `Image` do Tiptap vem ligada por padrão
  (prop `image`, default `true`).
- Interceptar paste e drop, subir, inserir o nó de imagem, com estado de "enviando" e de erro.
- Na exibição não há nada a fazer: o `<MDC>` já renderiza `![](url)` via `ProseImg`.

### Infra e settings

- `StorageSettings` no padrão do `SettingsBase` (bucket, region, endpoint, chaves, `PublicBaseUrl`),
  com a seção `"Storage"` no `appsettings.Copy.json`.
- Registrar o serviço real e manter o `FakeStorageService` em Testing e Development, seguindo o que o
  `ServicesConfigs` já faz com o `IEmailsService`.
- CORS no bucket liberando `PUT` a partir do domínio do front.
- A assinatura atual do `IStorageService` não comporta o que o fluxo precisa:
  `CreatePreSignedUrlForUpload(StorageContainer, string path)` não recebe content-type nem
  `Cache-Control`. Num PUT pré-assinado esses headers precisam entrar na assinatura, senão não dá
  para fixar o mime nem o TTL no momento do upload. Some-se o `Delete` do ciclo de vida: a interface
  vai mudar.

### Cache e entrega na borda (Cloudflare)

Só se aplica à opção 1 da decisão de acesso (bucket público). Nas opções 2 e 3 a imagem não passa
pelo CDN público e esta subseção inteira deixa de valer.

O egress do R2 já é zero, então **cache aqui não reduz custo** — não sobrou nada para reduzir. O
ganho é latência, mais as operações Class B ($0,36/milhão) que no volume do Estud já eram zero.
Ligar pela performance, não pela conta.

- **Custom domain é obrigatório.** O `r2.dev` é rate-limited, documentado como "apenas para
  desenvolvimento", e não suporta cache, WAF nem Access. Usar algo como `cdn.estud.com.br` — o zone
  do `estud.com.br` já está no Cloudflare.
- **Ao ligar o custom domain, desabilitar o `r2.dev`.** Com os dois ativos, o `r2.dev` continua
  servindo o bucket por fora e vira bypass de WAF e Access.
- **Imagem já é cacheada por padrão**: `jpg`, `jpeg`, `png`, `gif`, `webp` e `avif` estão na lista
  default de extensões. Não precisa de Cache Rule só para ligar o cache.
- **O TTL é que precisa de ajuste**: sem `Cache-Control` vindo da origem, o default é 120 minutos.
  Como o nome do arquivo é um Ulid e o conteúdo nunca muda sob aquele nome, o objeto é imutável — dá
  para gravar `Cache-Control: public, max-age=31536000, immutable` no upload sem risco de servir
  versão velha. Não existe invalidação porque não existe atualização, só arquivo novo.
- **Smart Tiered Cache** é a recomendação oficial para R2: escolhe sozinho o upper-tier mais próximo
  do bucket, então o miss na borda não vai direto ao R2.

**Image Transformations** cobre redimensionamento e conversão de formato sem pipeline próprio: o free
plan dá 5.000 transformações únicas por mês, funciona com imagem remota (inclusive R2) via URL
`/cdn-cgi/image/...`, e `format=auto` serve AVIF ou WebP conforme o browser. A cobrança é por
transformação única por mês calendário, independente de quantas vezes for servida; acima das 5.000,
$0,50 por mil. No volume modelado (~240 imagens/ano) é folga larga.

### Fora do escopo desta etapa

- Redimensionar ou comprimir por conta própria no backend — o Image Transformations resolve na borda,
  e só entra em cena se a decisão de acesso for a opção 1.
- Antivírus no arquivo enviado.
- Reaproveitar o upload em descrição de atividade e notificações.

## Fora do escopo

- Conteúdo ministrado (`TaughtContent`) e retificação com justificativa: a parte do diário com validade legal.
- Bloquear a edição do planejamento com a turma finalizada.
- Avisar os alunos quando o planejamento muda.
- Plano de ensino da disciplina (ementa, objetivos, bibliografia, critérios de avaliação).
