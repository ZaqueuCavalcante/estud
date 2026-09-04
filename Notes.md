# Plano: regra de média final configurável por instituição

Contexto: o cálculo de `AverageGrade` no `GetClassService` está errado — soma
`nota × peso` de **todos** os tipos de nota e divide por `100` fixo, enquanto o
teto de peso é validado **por `ClassNoteType`** (`CreateClassActivityService.cs:36-39`).
Uma turma com N1 + N2 + N3 pode chegar a 30 num campo documentado como "de 0 a 10".

Já existe `InstitutionConfig` (`NoteLimit`, `FrequencyLimit`) com endpoints
`GET/POST institutions/config`, tela em `Web/app/pages/configs.vue` e testes — a
regra de média encaixa exatamente aí.

## Modelo de cálculo (em duas camadas)

**Camada 1 — nota de cada tipo (N1, N2, N3), denominador fixo:**

```
Nx = Σ(nota_da_entrega × peso_da_atividade) / 100
```

Atividade não corrigida, entrega inexistente ou peso faltando valem zero — assim o
aluno sempre vê a nota final dele, independente do estágio da turma. Cai
naturalmente do `LEFT JOIN` + `Note = 0` default do `ClassActivityWork`.

**Camada 2 — média final a partir de N1/N2/N3**, conforme a regra da instituição.

## As regras hardcoded

Cada regra declara **quais tipos de nota ela usa**. Todo tipo declarado entra na
conta: se a turma não tem nenhuma atividade de N3 e a regra usa N3, N3 vale zero —
denominador fixo, coerente com a camada 1. Instituição que não trabalha com N3
escolhe uma regra que não declara N3.

| Regra | Notas | Fórmula |
|---|---|---|
| `BestTwoOfThree` *(default)* | N1, N2, N3 | média das **duas maiores** entre as três |
| `AverageOfTwo` | N1, N2 | `(N1 + N2) / 2` |
| `AverageOfThree` | N1, N2, N3 | `(N1 + N2 + N3) / 3` |
| `AverageOrThird` | N1, N2, N3 | `max( (N1+N2)/2 , N3 )` — N3 como substitutiva/exame |

Com N1=5.0, N2=8.0, N3=6.0: `BestTwoOfThree` → **7.0**, `AverageOfTwo` → **6.5**,
`AverageOfThree` → **6.3**, `AverageOrThird` → **6.5**.

`BestTwoOfThree` é o default e o valor de migração das instituições existentes.

### Consequência: a regra restringe quais notas podem ser lançadas

Com `AverageOfTwo`, criar atividade de N3 não faz sentido — a nota nunca entraria na
média. Então `rule.NoteTypes()` passa a valer também como validação de escrita
(Etapa 3) e como fonte do select do frontend (Etapa 6).

### Decisão pendente

**Trocar a regra da instituição para uma que usa menos notas, com atividades de N3 já
criadas, deve ser bloqueado?**

Sem bloqueio, notas já lançadas somem da média silenciosamente. Recomendação:
**recusar a troca** (`GradeRuleInUse`) se existir atividade de um tipo que a nova
regra não usa, em turma ainda não finalizada; a instituição apaga/reclassifica as
atividades antes. A alternativa é deixar trocar e simplesmente ignorar o tipo órfão —
mais simples, mas perde nota sem avisar.

---

## Etapa 1 — Domínio

**`Back/Domain/Enums/ClassGradeRule.cs`** *(novo)* — enum com `[Description]`,
valores inteiros explícitos (o `EnumUniqueValuesUnitTests` cobre isso):

```csharp
public enum ClassGradeRule
{
    [Description("Duas maiores entre N1, N2 e N3")]  BestTwoOfThree = 0,
    [Description("Média de N1 e N2")]                AverageOfTwo = 1,
    [Description("Média de N1, N2 e N3")]            AverageOfThree = 2,
    [Description("Média de N1 e N2, ou N3")]         AverageOrThird = 3,
}
```

**`Back/Domain/Classes/ClassGrade.cs`** *(novo)* — calculadora pura, sem I/O, mais o
mapa regra → tipos de nota:

```csharp
public static ClassNoteType[] NoteTypes(this ClassGradeRule rule)
public static decimal Final(IReadOnlyDictionary<ClassNoteType, decimal> notes, ClassGradeRule rule)
```

`Final` lê os tipos de `NoteTypes(rule)` e trata ausente como zero.

**`Back/Domain/Institutions/InstitutionConfig.cs`** — nova propriedade `GradeRule`,
`DefaultGradeRule = ClassGradeRule.BestTwoOfThree` no construtor, novo parâmetro em
`Setup(...)`. Enum grava como `int` (convenção padrão do EF no projeto), então o
`InstitutionConfigDbConfig` não muda.

> Sobre migration: não existe pasta `Migrations/` versionada hoje e o
> `HasMissingMigration()` está comentado no `IntegrationTestBase.cs:49` — o schema
> de teste vem do `EnsureCreatedAsync`. Nada a fazer no repo; fica o registro de que
> o banco de produção vai precisar da coluna quando as migrations voltarem.

## Etapa 2 — Leitura das notas (uma query, três consumidores)

Hoje o SQL de notas está duplicado dentro do `GetClassService`. Mover para
`Back/Database/EstudDbContext.Classes.cs`, no mesmo estilo do `GetTeacherId`,
agregando já no Postgres por `(turma, aluno, tipo_de_nota)`:

```sql
SELECT cs.class_id, cs.student_id, ca.note AS note_type,
       COALESCE(SUM(caw.note * ca.weight), 0) / 100 AS grade
FROM estud.classes__students cs
INNER JOIN estud.class_activities ca ON ca.class_id = cs.class_id
LEFT JOIN estud.class_activity_works caw
       ON caw.class_activity_id = ca.id AND caw.student_id = cs.student_id
WHERE cs.class_id = ANY({0})
GROUP BY cs.class_id, cs.student_id, ca.note
```

O `LEFT JOIN` cobre os dois casos de "vale zero" (entrega pendente e aluno
matriculado depois da criação da atividade). Tipo sem atividade nenhuma não volta na
query e vira zero na `ClassGrade.Final`. Aceitar uma lista de `classIds` deixa o
`GetStudentDetails` usar a mesma query.

## Etapa 3 — Aplicar nos consumidores

| Arquivo | Hoje | Depois |
|---|---|---|
| `GetClassService.cs:39-58` | soma tudo `/100` — estoura 10 | lê a config, agrupa por tipo, aplica a regra |
| `GetTeacherClassStudentsService.cs:20-32` | mock `Random(s.Id)` | cálculo real |
| `GetStudentDetailsService.cs:18-26` | mock `Random(student.Id)`, mesma nota em todas as turmas | nota real por turma + média das turmas |
| `CreateClassActivityService.cs` | aceita qualquer `ClassNoteType` | recusa tipo fora de `rule.NoteTypes()` → novo erro `NoteTypeNotUsedByInstitution` |

Sai o `Include(c => c.Activities)` do `GetClassService` (a query nova já traz o
peso), junto com o `First()` O(n²).

## Etapa 4 — API

- `SetupInstitutionConfigIn/Out`, `GetInstitutionConfigOut` + os dois mappers: campo
  `GradeRule` (serializa como string, via `EstudStringEnumConverter`).
- Os dois `Out` devolvem também `NoteTypes` (derivado da regra), pro frontend montar
  o select de nota sem duplicar o mapa.
- Validator do `SetupInstitutionConfigService`: `IsInEnum()` → novo erro
  `InvalidGradeRule` em `EstudErrors.Institutions.cs`; mais a checagem de troca de
  regra (`GradeRuleInUse`) conforme a decisão pendente. Ambos listados no
  `ErrorExamplesProvider` do controller.
- `<remarks>` dos dois controllers atualizado (hoje fala só de nota e frequência).
- Exemplos do `IApiDto` ganham as novas propriedades.

**Breaking change**: `SetupInstitutionConfigIn` é `POST` com corpo completo — cliente
que não mandar `GradeRule` cai no default do enum (`BestTwoOfThree`). A única
consumidora é a tela `configs.vue`, atualizada na Etapa 6.

## Etapa 5 — Testes

- **Unitários** (`Tests/Domain/ClassGradeUnitTests.cs`): as 4 regras; tipo declarado
  sem atividade valendo zero; empate nas duas maiores em `BestTwoOfThree`; turma sem
  atividade nenhuma; `NoteTypes()` de cada regra.
- **Integração** (`Tests/Features/Classes/GetClass/`): montando o cenário pelos
  endpoints (criar turma → atividades com peso → lançar notas), um teste por regra +
  um com atividade não corrigida valendo zero. Helper novo no
  `TestsHttpClient.Institutions.cs` para o parâmetro `gradeRule`.
- **`CreateClassActivity`**: recusa N3 quando a regra é `AverageOfTwo`.
- Ajustar os testes existentes de `GetInstitutionConfig`/`SetupInstitutionConfig` para
  os campos novos.

## Etapa 6 — Frontend

- `Web/app/types/configs.ts`: `gradeRule: string` e `noteTypes: string[]`.
- `configs/EditModal.vue`: `USelect` com as regras + descrição da fórmula, no schema
  Zod (`z.enum`, com `error` em português conforme o CLAUDE.md).
- `configs.vue`: terceiro card com a regra vigente.
- `classes/CreateActivityModal.vue:25-28`: o `noteOptions` hoje é fixo em N1/N2/N3 —
  passa a vir do `noteTypes` da config da instituição.

---

**Ordem de execução:** 1 → 2 → 3 → 4 → 5 → 6, com 5 podendo andar junto de 3.
