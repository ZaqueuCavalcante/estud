# Destaque dos termos encontrados na busca de planos de aula

A busca nos planos de aula (`EstudDbContext.SearchClassLessons`, usada pelo `GetTeacherClassLessons` e
pelo `GetStudentClassLessons`) já filtra as aulas com full text search do Postgres: `unaccent` nos dois
lados, stemming da config `portuguese` e prefixo (`:*`) em todos os termos. Falta mostrar **onde** o
termo apareceu:

- **na lista de aulas**: um trecho do plano com os termos destacados;
- **no plano aberto**: os termos destacados no `RichEditor`.

## O Nuxt não tem isso pronto

- O `UEditor` (base do `RichEditor`, inclusive no modo `readonly` da `DetailStudent.vue`) não tem busca
  nem destaque. O destaque do `UCommandPalette` é fuzzy (fuse.js) sobre listas no cliente, não serve.
- O `@tiptap/extension-highlight` é uma **mark**: altera o documento e o destaque seria salvo no plano se
  o professor editasse. O caminho certo são **decorations** do ProseMirror, que pintam sem tocar no
  conteúdo — uma extensão de ~30 linhas.

## Quem decide o que destacar é o back

O front não consegue reproduzir stemming, unaccent e prefixo. Substring no front diverge do Postgres:

| Busca | Postgres encontrou | Substring no front |
|---|---|---|
| `estudar` | "estudaram" (radical `estud`) | não destaca nada |
| `exercicio` | "exercício" (unaccent) | não destaca nada |
| `grafo` | "Grafos" | destaca só "Grafo" |

### 1. `matchedWords` — para destacar no plano completo

O back devolve as palavras **como aparecem no texto** (com acento e maiúsculas), e o front destaca por
comparação literal.

O `ts_debug` precisa rodar sobre o texto original, não sobre o `unaccent(planned_content)`, senão os
tokens voltam sem acento e não batem com o texto. O match é feito token a token, com a mesma tsquery do
`SearchClassLessons` (o `&&` entre lexemas não serve, porque não entende o `:*`):

```sql
SELECT DISTINCT t.token
FROM ts_debug('portuguese', planned_content) t
WHERE to_tsvector('portuguese', unaccent(t.token)) @@ <tsquery com prefixo>
-- busca "estudar grafo" → Grafos, grafo, estudaram
```

Operadores da busca não entram no destaque: com `grafo -ponderado`, só as palavras de `grafo` são
devolvidas; numa frase (`"busca em largura"`), cada palavra da frase é destacada onde aparecer.

O `RichEditor` ganha uma prop `highlight: string[]` que liga a extensão de decorations e destaca essas
palavras.

### 2. `snippet` — trecho para a lista de aulas

Via `ts_headline`, com a mesma tsquery:

```sql
ts_headline('portuguese', planned_content, <tsquery com prefixo>,
            'StartSel=«, StopSel=», MaxFragments=2, MaxWords=8')
-- hoje os alunos «estudaram» **«Grafos»** dirigidos e exercícios
```

- Marcadores próprios (`«»` ou caracteres de controle), não `<b>`. O front divide pelo marcador e
  renderiza com `<mark>`, **sem `v-html`** (o plano é texto do professor; `v-html` abre XSS).
- O `ts_headline` roda sobre o Markdown cru e sobram `**` e sintaxe de imagem. Limpar com
  `regexp_replace` antes da função, ou no front.

**Problema em aberto — acento:** a tsquery tem lexemas sem acento (`exercici`), então o `ts_headline`
sobre o texto original não marca "exercício". Opções:

- rodar sobre `unaccent(planned_content)`: marca certo, mas o trecho aparece sem acento na tela;
- montar o trecho a partir das posições das `matchedWords` no texto original (no back ou no front),
  sem `ts_headline`.

## Contrato

Só acrescenta campos nos itens da resposta do `GetTeacherClassLessons` e do `GetStudentClassLessons`,
preenchidos apenas quando há `search`:

```csharp
public List<string> MatchedWords { get; set; } = [];
public string? Snippet { get; set; }
```

O filtro e o endpoint não mudam. A tsquery com prefixo, hoje inline no `SearchClassLessons`, passa a ser
reaproveitada pelo `ts_debug` e pelo `ts_headline`.

## Cenários de teste

`matchedWords` e `snippet` são testáveis via API:

| Cenário | Plano | Busca | `matchedWords` |
|---|---|---|---|
| Stemming | "os alunos estudaram grafos" | `estudar grafo` | `estudaram`, `grafos` |
| Acento | "Lista de exercício" | `exercicio` | `exercício` |
| Prefixo | "Exercício número sete" | `exerci` | `Exercício` |
| Exclusão | "grafo dirigido" | `grafo -ponderado` | `grafo` |
| Sem busca | qualquer | — | vazio, `snippet` nulo |
