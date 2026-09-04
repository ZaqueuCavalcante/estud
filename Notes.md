# Plano: regra de média final configurável por instituição

## Decisão pendente

**Trocar a regra da instituição para uma que usa menos notas, com atividades de N3 já
criadas, deve ser bloqueado?**

Sem bloqueio, notas já lançadas somem da média silenciosamente. Recomendação:
**recusar a troca** (`GradeRuleInUse`) se existir atividade de um tipo que a nova
regra não usa, em turma ainda não finalizada; a instituição apaga/reclassifica as
atividades antes. A alternativa é deixar trocar e simplesmente ignorar o tipo órfão —
mais simples, mas perde nota sem avisar.

## Etapa 2 — Compartilhar a query de notas

A query já existe, mas privada no `GetClassService` (`GetClassStudentsWorks`, filtrando
por um `class_id` só). Mover para `Back/Database/EstudDbContext.Classes.cs`, no mesmo
estilo do `GetTeacherId`, trocando o `WHERE cs.class_id = {0}` por
`WHERE cs.class_id = ANY({0})` e devolvendo também o `class_id` — assim
`GetTeacherClassStudents` e `GetStudentDetails` usam a mesma query.

## Etapa 3 — Aplicar nos consumidores

| Arquivo | Hoje | Depois |
|---|---|---|
| `GetTeacherClassStudentsService.cs:20-38` | mock `Random(s.Id)` | cálculo real |
| `GetStudentDetailsService.cs:18-30` | mock `Random(student.Id)`, mesma nota em todas as turmas | nota real por turma + média das turmas |
| `CreateClassActivityService.cs` | aceita qualquer `ClassNoteType` | recusa tipo fora de `rule.NoteTypes` → novo erro `NoteTypeNotUsedByInstitution` |

Os dois primeiros seguem o `GetClassService`: `config.GradeRule.Average(works)` dentro
de `Math.Round(..., 1, MidpointRounding.AwayFromZero)`.

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

## Etapa 5 — Testes de integração

Os unitários do `ClassGrade` já estão prontos. Falta:

- **`Tests/Features/Classes/GetClass/`**: um teste por regra — os seis casos que
  existem cobrem só a default. Depende do helper abaixo.
- Helper novo no `TestsHttpClient.Institutions.cs`: parâmetro `gradeRule` no
  `SetupInstitutionConfig`.
- **`GetTeacherClassStudents`** e **`GetStudentDetails`**: nota real no lugar do mock.
- **`CreateClassActivity`**: recusa N3 quando a regra é `AverageOfTwo`.
- Ajustar os testes existentes de `GetInstitutionConfig`/`SetupInstitutionConfig` para
  os campos novos.

## Etapa 6 — Frontend

- `Web/app/types/configs.ts`: `gradeRule: string` e `noteTypes: string[]`.
- `configs/EditModal.vue`: `USelect` com as regras + descrição da fórmula, no schema
  Zod (`z.enum`, com `error` em português conforme o CLAUDE.md).
- `configs.vue`: terceiro card com a regra vigente.
- `classes/CreateActivityModal.vue:25`: o `noteOptions` hoje é fixo em N1/N2/N3 —
  passa a vir do `noteTypes` da config da instituição.

---

**Ordem de execução:** 1 → 2 → 3 → 4 → 5 → 6, com 5 podendo andar junto de 3.
