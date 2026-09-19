# Full text search nos planos de aula

Agora o professor escreve texto rico no plano de uma aula (`ClassLesson.PlannedContent`, `string?` de
até 10k caracteres, gravado em **Markdown** pelo `RichEditor` com `content-type="markdown"`). A ideia é
permitir buscar nesses planos dentro de uma turma usando o full text search do Postgres.

- **v1**: `tsvector` calculado na hora da consulta + `@@` no parâmetro `search` do endpoint de listagem
  de aulas. Sem coluna nova, sem índice, sem configuração de busca própria.
- **v2**: destaque dos termos encontrados (trecho na lista e highlight no plano aberto).

## Como funciona

### `tsvector`: o documento processado

O Postgres quebra o texto em tokens, descarta stopwords ("os", "de", "a") e reduz cada palavra ao
radical (stemming), guardando a posição:

```sql
select to_tsvector('portuguese', 'Os alunos estudaram grafos dirigidos');
-- 'alun':2 'dirig':5 'estud':3 'graf':4
```

"grafo", "grafos" e "Grafos" viram o mesmo lexema `graf`, coisa que o `ILIKE '%...%'` não faz.

### `tsquery`: a busca passa pelo mesmo processo

```sql
select websearch_to_tsquery('portuguese', 'grafo dirigido -ponderado');
-- 'graf' & 'dirig' & !'ponder'
```

O `websearch_to_tsquery` aceita sintaxe de buscador (aspas para frase, `-` para excluir, `or`) e nunca
dá erro de sintaxe, então o texto digitado pelo usuário vai direto.

### `@@`: o match

```sql
where class_id = @classId
  and to_tsvector('portuguese', unaccent(planned_content))
      @@ websearch_to_tsquery('portuguese', unaccent(@search))
```

## Decisões

### Sem coluna `tsvector` e sem GIN

A busca é sempre dentro de uma turma, que tem no máximo algumas dezenas de aulas. O planner filtra por
`class_id` primeiro e aplica o `@@` nessas poucas linhas; um índice GIN não seria usado. Calcular o
`to_tsvector` na hora sobre ~30 textos de até 10k caracteres custa poucos milissegundos.

Sem coluna gerada: nada muda na tabela `class_lessons`, não há custo na escrita e não existe vetor
gravado que possa divergir da forma de processar o texto.

Se um dia houver busca entre turmas ("todas as minhas turmas", instituição inteira), aí sim entra uma
coluna gerada `STORED` com índice GIN — e, para ela, uma configuração de busca própria com o `unaccent`
como dicionário, já que coluna gerada só aceita funções `IMMUTABLE` e `unaccent(text)` é `STABLE`.

### Acentos: função `unaccent` nos dois lados

A config `portuguese` não remove acentos, então "exercicio" não encontra "exercício". A extensão
`unaccent` resolve, aplicada como função tanto no texto quanto no termo buscado. Como o vetor é
calculado na hora, a volatilidade `STABLE` do `unaccent` não é problema.

O `unaccent` já vem na imagem oficial do Postgres. A extensão é declarada no modelo com
`modelBuilder.HasPostgresExtension("unaccent")`: a migration gera o `CREATE EXTENSION` e o
`EnsureCreatedAsync()` dos testes também cria a extensão, então o setup dos testes não muda.

### URLs de imagem: nada a fazer

Os tokens de URL das imagens (`![](https://cdn.../grafo.png)`) entram como lexema inteiro
(`'cdn.estud.com'`, `'/lessons/grafo.png)'`...) e só casam se digitarem exatamente aquela string.
Buscas por `grafo`, `estud` e `lessons` num plano que só tem a imagem não encontram nada. Nomes como
`main.py` são classificados como `host` e continuam encontráveis.

Os símbolos de Markdown (`#`, `*`, `-`) o parser já ignora; não precisa converter o Markdown.

### Termo de busca com no mínimo 3 letras

Termos mais curtos são rejeitados na validação.

### Busca só com stopwords: sem tratamento por enquanto

Termos como `para`, `como` ou `que` passam no mínimo de 3 letras, mas são stopwords: o
`websearch_to_tsquery` devolve query vazia e o `@@` dá `false` para todas as aulas, então a lista volta
vazia. Fica assim para ver o comportamento na prática antes de decidir (tratar como "sem busca" via
`numnode(query) = 0`, ou devolver erro).

### Busca por prefixo: `:*` em todos os termos

O `websearch_to_tsquery` não faz prefixo ("exerci" não encontra "exercício"). A busca roda o
`websearch_to_tsquery` primeiro (unaccent, stemming, stopwords, aspas, `-`, `or`), acrescenta `:*` em
cada lexema do texto resultante e usa `to_tsquery('simple', ...)` para não aplicar o stemming de novo:

```sql
select websearch_to_tsquery('portuguese', unaccent('exerci -ponderado'))::text;
-- 'exerc' & !'ponder'   →   to_tsquery('simple', '''exerc'':* & !''ponder'':*')
```

Tudo roda numa consulta só: o filtro é SQL via `FromSql` (`EstudDbContext.SearchClassLessons`), com o
`regexp_replace` colocando o `:*`, e o resto da query (turma, ordem, projeção) continua em LINQ. Efeito colateral: o prefixo amplia os resultados ("grafo" vira
`graf:*` e também encontra "grafite"). A busca continua sendo disparada só depois do debounce do front.

## v1 — implementação

### Back

`EstudDbContext.OnModelCreating`:

```csharp
modelBuilder.HasPostgresExtension("unaccent");
```

Migration só com o `CREATE EXTENSION IF NOT EXISTS unaccent`, gerada pelo EF.

Endpoint: o `GetTeacherClassLessons` (`Back/Features/Teachers/GetTeacherClassLessons/`) troca o filtro
`ILIKE` atual pelo `@@`, **mantendo a ordem por número**:

```csharp
.Where(l => l.PlannedContent != null
    && EF.Functions.ToTsVector("portuguese", EF.Functions.Unaccent(l.PlannedContent))
        .Matches(EF.Functions.WebSearchToTsQuery("portuguese", EF.Functions.Unaccent(search))))
```

O mesmo filtro vale para o aluno (`GetStudentClassLessons`) que tbm pode pesquisar nos planos de aula da sua turma.

Validação: termo de busca com no mínimo 3 e no máximo N caracteres (novo erro `Invalid...` em
`EstudInvalidErrors.cs`).

### Front

A tela da turma continua igual, com o campo de busca que já alimenta o `search`.

### Cenários de teste

Rodam contra Postgres real, então stemming, acento e sintaxe são exercitados de verdade.

- **Authentication / Authorization**: iguais aos existentes do `GetTeacherClassLessons`.
- **Validation errors**: termo de busca abaixo de 3 letras e acima do tamanho máximo.
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
