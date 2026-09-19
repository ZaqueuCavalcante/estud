# Full text search nos planos de aula

Agora o professor escreve texto rico no plano de uma aula (`ClassLesson.PlannedContent`, `string?` de
até 10k caracteres, gravado em **Markdown** pelo `RichEditor` com `content-type="markdown"`). A ideia é
permitir buscar nesses planos dentro de uma turma usando o full text search do Postgres, com índice GIN.

- **v1**: coluna `tsvector` gerada + índice GIN + parâmetro `search` no endpoint de listagem de aulas.
- **v2**: destaque dos termos encontrados (trecho na lista e highlight no plano aberto).

## Como funciona

### `tsvector`: o documento processado

O Postgres quebra o texto em tokens, descarta stopwords ("os", "de", "a") e reduz cada palavra ao
radical (stemming), guardando a posição:

```sql
select to_tsvector('portuguese', 'Os alunos estudaram grafos dirigidos');
-- 'alun':2 'dirig':5 'estud':3 'graf':4
```

"grafo", "grafos" e "Grafos" viram o mesmo lexema `graf`, coisa que o `ILIKE '%...%'` (usado hoje em
`GetClassesService`) não faz.

### `tsquery`: a busca passa pelo mesmo processo

```sql
select websearch_to_tsquery('portuguese', 'grafo dirigido -ponderado');
-- 'graf' & 'dirig' & !'ponder'
```

O `websearch_to_tsquery` aceita sintaxe de buscador (aspas para frase, `-` para excluir, `or`) e nunca
dá erro de sintaxe, então o texto digitado pelo usuário vai direto.

### `@@`: o match

```sql
where search_vector @@ websearch_to_tsquery('estud.pt', 'grafos')
order by ts_rank(search_vector, query) desc
```

### Índice GIN

Índice invertido, como o índice remissivo de um livro:

```
'graf'  → [lesson 12, lesson 40, lesson 311]
'dirig' → [lesson 40, lesson 97]
```

Para `graf & dirig`, o Postgres intersecta as listas sem abrir as linhas. O padrão é guardar o
`tsvector` numa **coluna gerada `STORED`** e indexar essa coluna, assim o vetor é calculado uma vez, no
`INSERT`/`UPDATE`. O custo fica na escrita (amenizado pela *pending list* do `fastupdate`), o que não
pesa para um plano de aula que é editado de vez em quando.

### Ressalva: dentro de uma turma o GIN quase não entra

Uma turma tem no máximo algumas dezenas de aulas. O planner filtra por `class_id` e aplica o `@@`
nessas poucas linhas, então o GIN provavelmente nem é usado. Nesse escopo o ganho do FTS é
**linguístico** (radicais, acentos, stopwords, ranking), não de desempenho. O GIN passa a importar em
buscas que cruzam turmas ("todas as minhas turmas", instituição inteira). Criar o índice agora custa
pouco e já deixa isso pronto.

## Decisões

### Acentos: configuração `estud.pt` com `unaccent` como dicionário

A config `portuguese` não remove acentos, então "exercicio" não encontra "exercício". A solução é a
extensão `unaccent`, mas **como dicionário dentro de uma configuração de busca**, não como função:

```sql
create extension if not exists unaccent;
create text search configuration estud.pt (copy = portuguese);
alter text search configuration estud.pt
  alter mapping for hword, hword_part, word with unaccent, portuguese_stem;
```

Por quê: coluna gerada `STORED` e expressão de índice só aceitam funções `IMMUTABLE` (o valor é gravado
em disco; se a função pudesse mudar o resultado depois, o gravado e o recalculado deixariam de bater e a
busca erraria sem aviso). Volatilidade conferida no Postgres local:

| Função | Volatilidade |
|---|---|
| `to_tsvector(regconfig, text)` | `IMMUTABLE` — pode |
| `to_tsvector(text)` | `STABLE` — depende de `default_text_search_config` |
| `unaccent(text)` | `STABLE` — depende do `search_path` para achar o dicionário |

Então `to_tsvector('portuguese', unaccent(planned_content))` é recusado. Com o `unaccent` dentro da
config, a única chamada é `to_tsvector(regconfig, text)`, que é imutável.

O `unaccent` é um dicionário de *filtro*: normaliza o token e repassa ao próximo da cadeia.
"exercício" (tipo `word`) vira `exercicio` e cai no mesmo radical de "exercicio" digitado sem acento
(tipo `asciiword`, que nem passa pelo unaccent).

**Efeito colateral:** se a config `estud.pt` mudar um dia, os vetores gravados não se recalculam
sozinhos. A migration que alterar a config precisa forçar o recálculo:
`UPDATE class_lessons SET planned_content = planned_content`.

O `unaccent` já vem na imagem oficial do Postgres; nada muda no ambiente.

### URLs de imagem: **não** dropar mappings

A ideia inicial era `drop mapping for url, url_path, host, file, email` para não indexar as URLs das
imagens (`![](https://cdn.../grafo.png)`). Descartada depois de testar com `ts_debug`:

```
asciiword | host                              ← a palavra solta é asciiword, não host
url       | cdn.estud.com/lessons/grafo.png)
host      | cdn.estud.com                     ← só vira host com formato de domínio
url_path  | /lessons/grafo.png)
host      | main.py                           ← o problema
```

- O tipo de token é a classificação pelo **formato**, não a palavra. "host" no texto continua
  encontrável; prefixos não mudariam nada.
- O risco real é o contrário: `main.py`, `index.html`, `estud.com.br` são classificados como `host`.
  Com o drop, um plano que cita `main.py` não seria encontrado.
- O drop nem é necessário: os tokens de URL entram como lexema inteiro (`'cdn.estud.com'`,
  `'/lessons/grafo.png)'`...) e só casam se digitarem exatamente aquela string. Buscas por `grafo`,
  `estud` e `lessons` num plano que só tem a imagem retornaram `false`. Custo: um pouco mais de espaço
  no índice.

Os símbolos de Markdown (`#`, `*`, `-`) o parser já ignora; não precisa converter o Markdown.

### Busca por prefixo: fora da v1

O `websearch_to_tsquery` não faz prefixo ("graf" não encontra "grafos" enquanto digita). Busca
enquanto digita exigiria montar `to_tsquery` com `:*` em cada termo. Fica para depois.

### Busca só com stopwords

`de`, `o que`: o `websearch_to_tsquery` devolve query vazia e o `@@` dá `false` para tudo. Proposta:
tratar como "sem busca" e devolver todas as aulas (lista vazia pareceria bug para o professor).
**Decidido:** tratar como sem busca.

## v1 — implementação

### Back

`ClassLesson.cs` (`Back/Domain/Classes/`):

```csharp
public NpgsqlTsVector SearchVector { get; private set; }
```

`ClassLessonDbConfig.cs` (`Back/Database/Classes/`):

```csharp
entity.HasGeneratedTsVectorColumn(e => e.SearchVector, "estud.pt", e => new { e.PlannedContent })
    .HasIndex(e => e.SearchVector)
    .HasMethod("GIN");
```

SQL da config numa constante no `Back` (ex.: `FullTextSearchSql.Setup`), usada **pela migration e pelo
setup dos testes**, para o banco de teste nunca divergir da produção.

Migration: `migrationBuilder.Sql(FullTextSearchSql.Setup)` **antes** da coluna. O EF gera sozinho o
`GENERATED ALWAYS AS (to_tsvector('estud.pt', ...)) STORED` e o `CREATE INDEX ... USING GIN`.

Endpoint: parâmetro opcional `search` no `GetTeacherClassLessons`
(`Back/Features/Teachers/GetTeacherClassLessons/`), filtrando as aulas e **mantendo a ordem por
número**:

```csharp
.Where(l => l.ClassId == classId
    && l.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("estud.pt", search)))
```

Validação: tamanho máximo do termo de busca (novo erro `Invalid...` em `EstudInvalidErrors.cs`).

### Front

A tela da turma continua igual, só com um campo de busca que alimenta o `search`.

### Testes — ajuste obrigatório no setup do banco

`Tests/Base/IntegrationTestBase.cs` cria o banco com `EnsureDeletedAsync()` + `EnsureCreatedAsync()`,
que **ignora migrations**. Sem ajuste, o `CREATE TABLE class_lessons` falha com
`text search configuration "estud.pt" does not exist` e **todos** os testes quebram.

Correção: separar o `EnsureCreated` nas duas etapas que ele já faz por dentro e rodar a config no meio:

```csharp
var creator = ctx.GetService<IRelationalDatabaseCreator>();
await creator.CreateAsync();
await ctx.Database.ExecuteSqlRawAsync(FullTextSearchSql.Setup);
await creator.CreateTablesAsync();
```

### Cenários de teste

Rodam contra Postgres real, então stemming, acento e sintaxe são exercitados de verdade.

- **Authentication / Authorization**: iguais aos existentes do `GetTeacherClassLessons`.
- **Validation errors**: termo de busca acima do tamanho máximo.
- **Happy path**: cada linha é um teste que monta os planos com `UpdateLessonPlan` e busca com
  `GetTeacherClassLessons(search: ...)`.

| Cenário | Plano | Busca | Esperado |
|---|---|---|---|
| Stemming | "grafos dirigidos" | `grafo` | encontra |
| Acento no texto | "exercício" | `exercicio` | encontra |
| Acento na busca | "exercicio" | `exercício` | encontra |
| Maiúsculas | "Grafos" | `grafos` | encontra |
| Várias palavras (AND) | "grafos dirigidos" | `grafo árvore` | não encontra |
| Frase | "busca em largura" | `"busca em largura"` | encontra só na ordem certa |
| Exclusão | 2 aulas com "grafo", uma com "ponderado" | `grafo -ponderado` | só a outra |
| `or` | aulas com "grafo" e com "árvore" | `grafo or árvore` | as duas |
| URL de imagem | só `![](https://cdn.../grafo.png)` | `grafo` | não encontra |
| Nome tipo host | "rodar main.py" | `main.py` | encontra |
| Plano editado | troca "grafo" por "árvore" | `grafo` / `árvore` | só o novo termo |
| Plano apagado | plano limpo | termo antigo | não encontra |
| Isolamento de turma | mesmo termo em outra turma | termo | só a turma pedida |
| Sem busca | — | `search` vazio | todas as aulas, como hoje |
| Só stopwords | "grafos" | `de` | todas as aulas (se confirmada a decisão acima) |

Fora dos testes: se o GIN é de fato usado. Com poucas linhas o planner escolhe seq scan de qualquer
jeito, e isso é performance, não comportamento. No máximo um assert em `pg_indexes`; a sugestão é não
ter.

## v2 — destaque dos termos encontrados

O Nuxt não tem isso pronto:

- O `UEditor` (base do `RichEditor`, inclusive no modo `readonly` da `DetailStudent.vue`) não tem busca
  nem destaque. O destaque do `UCommandPalette` é fuzzy (fuse.js) sobre listas no cliente, não serve.
- O `@tiptap/extension-highlight` é uma **mark**: altera o documento e o destaque seria salvo no plano se
  o professor editasse. O caminho certo são **decorations** do ProseMirror, que pintam sem tocar no
  conteúdo — uma extensão de ~30 linhas.

### Quem decide o que destacar é o back

O front não consegue reproduzir stemming e unaccent. Substring no front diverge do Postgres:

| Busca | Postgres encontrou | Substring no front |
|---|---|---|
| `estudar` | "estudaram" (radical `estud`) | não destaca nada |
| `exercicio` | "exercício" (unaccent) | não destaca nada |
| `grafo` | "Grafos" | destaca só "Grafo" |

**1. `matchedWords` — para destacar no plano completo.** O back devolve as palavras como aparecem no
texto:

```sql
select distinct t.token
from ts_debug('estud.pt', planned_content) t
where t.lexemes && <lexemas da query>
-- busca "estudar grafo" → Grafos, grafo, estudaram
```

O `RichEditor` ganha uma prop `highlight` que liga a extensão de decorations e destaca essas palavras
por comparação literal.

**2. `snippet` — trecho para a lista de aulas.** Via `ts_headline`:

```sql
ts_headline(..., 'StartSel=«, StopSel=», MaxFragments=2, MaxWords=8')
-- hoje os alunos «estudaram» **«Grafos»** dirigidos e exercícios
```

- Marcadores próprios (`«»` ou caracteres de controle), não `<b>`. O front divide pelo marcador e
  renderiza com `<mark>`, **sem `v-html`** (o plano é texto do professor; `v-html` abre XSS).
- O `ts_headline` roda sobre o Markdown cru e sobram `**` e sintaxe de imagem. Limpar com
  `regexp_replace` antes da função, ou no front.

A v2 só acrescenta campos na resposta; coluna, índice e endpoint da v1 não mudam. `matchedWords` e
`snippet` também são testáveis via API.

## Decisões tomadas

- A busca vale também para o aluno (`GetStudentClassLessons` / `GetStudentClassLesson`).
- Busca só com stopwords é tratada como "sem busca": devolve todas as aulas.

## Estado atual (v0, sem FTS)

Para ver a feature funcionando antes do FTS, o `GetTeacherClassLessons` ganhou um parâmetro opcional
`search` que filtra com `ILIKE '%termo%'` sobre o `PlannedContent` (sem `tsvector`, sem GIN, sem
`unaccent`), e a aba de aulas do professor ganhou um campo de busca. A v1 acima troca o filtro `ILIKE`
pelo `@@` mantendo o mesmo contrato do endpoint.
