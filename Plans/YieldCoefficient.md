# YieldCoefficient

Plano de implementação do cálculo do coeficiente de rendimento (CR) do aluno, seguindo a mesma
estrutura do `ClassGradeRule`: um enum de regras, um algoritmo de domínio por regra, a regra
escolhida no `InstitutionConfig` e o cálculo aplicado na leitura.

## Decisões

**O CR é calculado na leitura, não gravado.** Hoje `EstudStudent.YieldCoefficient` é uma coluna
`numeric(4,2)` que nunca é escrita por ninguém — `GetStudentDetails` só a devolve, sempre `0`.
A coluna sai e o CR passa a ser derivado, igual ao `AverageGrade`.

**A frequência não entra na conta.** No CR clássico das IES brasileiras o coeficiente é sempre
`Σ(nota × peso) / Σ(peso)`. A frequência é critério de aprovação, não variável de rendimento: ela
decide o desfecho do aluno na disciplina, e chega ao CR por meio desse desfecho — trabalho do
fechamento de turma, que ainda não existe.

**Quais turmas entram — decidir na implementação.** O ideal conceitual é só `ClassStatus.Finalized`,
mas não existe fechamento de turma (nada seta `Finalized` nem `Aprovado`/`Reprovado`, como o próprio
`Web/app/pages/dev/class-lifecycle.vue` documenta). Restringir a `Finalized` faz o CR nascer sempre
`0` e impede os testes de integração de montarem o cenário pela API. A recomendação é contar
`Started` + `Finalized` — CR acumulado, incluindo o parcial das turmas em curso —, que converge para
o CR clássico quando o `FinalizeClass` existir.

## 1. Domínio

### `Back/Domain/Enums/YieldCoefficientRule.cs` (novo)

```csharp
public enum YieldCoefficientRule
{
    [Description("Média ponderada pela carga horária das disciplinas")]
    WeightedByWorkload = 0,

    [Description("Média simples das médias finais")]
    SimpleAverage = 1,

    [Description("Média ponderada pelos créditos das disciplinas")]
    WeightedByCredits = 2,
}
```

### `Back/Domain/Students/YieldCoefficient.cs` (novo)

`static class` no molde do `Back/Domain/Classes/ClassGrade.cs`, com `extension(YieldCoefficientRule rule)`:

```csharp
public decimal Calculate(IEnumerable<(decimal Grade, int Workload, int Credits)> disciplines)
```

- Validação num helper privado, como o `Notes(...)` do `ClassGrade`: `Grade` entre 0 e 10,
  `Workload` e `Credits` não negativos — `ArgumentOutOfRangeException` fora disso.
- Lista vazia devolve `0`.
- Soma dos pesos igual a `0` cai para média simples, evitando divisão por zero quando a grade não
  tem carga horária ou créditos preenchidos.
- `switch` sobre a regra, com
  `_ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown YieldCoefficientRule!")`.
- Sem arredondamento aqui — quem arredonda é o service, igual ao `ClassGrade`.

### `Back/Domain/Institutions/InstitutionConfig.cs`

Nova propriedade `YieldRule`, `public const YieldCoefficientRule DefaultYieldRule =
YieldCoefficientRule.WeightedByWorkload`, atribuição no construtor sem parâmetro e novo parâmetro
no `Setup(...)`.

### Remoção da coluna

- `Back/Domain/Students/EstudStudent.cs` — remover `public decimal YieldCoefficient`.
- `Back/Database/Students/EstudStudentDbConfig.cs` — remover o `HasPrecision(4, 2)` dela.

## 2. Erro

`Back/Errors/EstudErrors.Institutions.cs` — `InvalidYieldCoefficientRule`, no molde do
`InvalidClassGradeRule`, com mensagem "Regra de cálculo de coeficiente de rendimento inválida.".

## 3. Config da instituição

Mesmos arquivos que o `GradeRule` tocou:

| Arquivo | Mudança |
|---|---|
| `SetupInstitutionConfigIn.cs` | `YieldRule` + os dois exemplos |
| `SetupInstitutionConfigOut.cs` | `YieldRule` + exemplos |
| `SetupInstitutionConfigMapper.cs` | mapear `YieldRule` |
| `SetupInstitutionConfigService.cs` | `RuleFor(x => x.YieldRule).IsInEnum().WithError(InvalidYieldCoefficientRule.I)` e passar para o `config.Setup(...)` |
| `SetupInstitutionConfigController.cs` | `InvalidYieldCoefficientRule` no `ErrorExamplesProvider` e `<remarks>` atualizado |
| `GetInstitutionConfigOut.cs` / `GetInstitutionConfigMapper.cs` / `GetInstitutionConfigController.cs` | idem, sem validação |

Nenhuma policy nova.

## 4. `GetStudentDetails`

O `GetStudentDetailsService` já calcula a média por turma com `config.GradeRule.Average(...)`.
Falta resolver os pesos e aplicar a regra:

1. No vínculo ativo mais recente (já buscado em `GetCourse`), expor também o `CourseCurriculumId`.
2. Carregar os `CourseCurriculumDisciplines` da grade num dicionário
   `DisciplineId → (Credits, Workload)`.
3. `GetClasses` passa a projetar também `cs.Class!.DisciplineId`.
4. Montar as tuplas a partir das turmas `Started`/`Finalized` e fazer
   `Round(config.YieldRule.Calculate(items))`.

**Cuidado com a carga horária:** existem duas, em unidades diferentes —
`CourseCurriculumDiscipline.Workload` (`ushort`, horas, a curricular) e `Class.Workload` (`int`,
minutos, acumulado em `Class.CreateLessons`). O peso do CR é a curricular. Para turma de disciplina
fora da grade do aluno, o fallback é `Class.Workload / 60` e `Credits = 1`.

`GetStudentDetailsOut.YieldCoefficient` mantém a assinatura — muda só a origem, de
`student.YieldCoefficient` para o valor calculado.

## 5. Testes

- **`Tests/Domain/YieldCoefficientUnitTests.cs`** (novo) — mesma estrutura do
  `ClassGradeUnitTests`: `#region Cenários` com as listas estáticas (histórico completo, disciplina
  única, cargas desiguais, notas perfeitas, tudo zero, lista vazia, pesos zerados), depois uma
  região por eixo, `[TestCase]` por regra onde as três devem coincidir (nota perfeita → `10`), e
  asserts com o cálculo em comentário: `// (8×60 + 6×30) / 90`.
- **`Tests/Clients/TestsHttpClient.Institutions.cs`** — parâmetro
  `YieldCoefficientRule yieldRule = YieldCoefficientRule.WeightedByWorkload` no
  `SetupInstitutionConfig`.
- **`SetupInstitutionConfigIntegrationTests`** — em *Validation errors*, regra inválida →
  `ShouldBeError(InvalidYieldCoefficientRule.I)`; em *Happy path*, salvar e reler.
- **`GetInstitutionConfigIntegrationTests`** — default `WeightedByWorkload`.
- **`GetStudentDetailsIntegrationTests`** — cenário montado pelos endpoints (curso, grade com cargas
  diferentes, turmas, atividades, notas) e um teste por regra, trocando a config via
  `SetupInstitutionConfig`.

## 6. Front

| Arquivo | Mudança |
|---|---|
| `Web/app/types/configs.ts` | `yieldRule: string` |
| `Web/app/utils/configs.ts` | `yieldCoefficientRules` (mesmo shape `label`/`description`/`example` com fração) e `yieldCoefficientRuleOptions` |
| `Web/app/components/configs/YieldRuleExplanation.vue` (novo) | cópia estrutural do `GradeRuleExplanation.vue` |
| `Web/app/components/configs/EditModal.vue` | campo zod `z.string({ error: 'Campo obrigatório' }).min(1, 'Campo obrigatório')`, `USelect` com a explicação e carregamento no `watch(open)` |
| `Web/app/pages/configs.vue` | linha do CR ao lado da regra de média |
| `Web/app/components/students/DetailManager.vue` | nada — já renderiza `yieldCoefficient` |
| `Web/app/utils/db-schema.ts` | tirar `yield_coefficient` de `students`; acrescentar `yield_rule` em `institution_configs` (e o `grade_rule`, que ficou faltando) |

## 7. Banco

Não há migrations versionadas no repo e os testes usam `EnsureCreatedAsync`, então o trabalho é
gerar o script pelo fluxo do `Back/Database/Migrations.md` na hora de subir: `institution_configs`
ganha `yield_rule integer not null default 0` e `students` perde `yield_coefficient`.

## Ordem sugerida

1. Enum + `YieldCoefficient.cs` + testes unitários — fecha o algoritmo isolado.
2. `InstitutionConfig` + erro + endpoints de config + testes.
3. `GetStudentDetails` + remoção da coluna.
4. Front.
