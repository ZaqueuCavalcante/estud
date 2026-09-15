# Planejamento de aula

Primeiro passo do diário de classe: o professor escreve o que vai ser abordado em cada aula, e o
aluno vê esse planejamento. O conteúdo ministrado (o registro com validade legal) e a retificação
ficam para depois.

## Back-end

### 1. Domínio

`Back/Domain/Classes/ClassLesson.cs`

- Novo campo `public string? PlannedContent { get; set; }`.
- Novo método `UpdatePlan(string? content)`, que grava `null` quando `content.IsEmpty()` e o texto
  com `Trim()` nos outros casos. Precisa ser `IsEmpty()`: o `HasValue()` devolve `true` para uma
  string só com espaços (`Back/Extensions/StringExtensions.cs:33`).

O `ClassLessonDbConfig` fica como está. A coluna sai como `planned_content text null` pelo snake
case, e o limite de tamanho fica no validator, como nos outros textos do projeto.

### 2. Feature `Teachers/UpdateLessonPlan`

`Back/Features/Teachers/UpdateLessonPlan/` com Controller, Service e In, no mesmo formato do
`CreateLessonAttendance`.

- `PUT teachers/lessons/{lessonId}/plan`, `Authorize(Policies.UpdateLessonPlan)`. Responde com
  `SuccessOut`.
- `UpdateLessonPlanIn { string? PlannedContent }`, com exemplo.
- Service:
  1. Validator: `MaximumLength(2000).WithError(InvalidClassLessonPlan.I)`.
  2. Aula por `l.Id == lessonId && l.Class.InstitutionId == institutionId` → `ClassLessonNotFound`.
  3. Professor vinculado via `ctx.ClassTeachers` → `TeacherNotAssignedToClass`.
  4. `lesson.UpdatePlan(data.PlannedContent)` e `SaveChangesAsync`.
- O planejamento pode ser editado em qualquer aula da turma, passada ou futura, com ou sem chamada.
  A chamada bloqueia aula futura; o planejamento é justamente para aula futura. Enviar vazio limpa
  o planejamento.
- Policy em `Back/Auth/Policies/Policies.Teachers.cs`: `UpdateLessonPlan`, `UserType.Teacher`, no
  bloco do `CreateLessonAttendance`.
- Erro novo em `Back/Errors/EstudErrors.Classes.cs`: `InvalidClassLessonPlan`, "Planejamento da aula
  inválido.".

### 3. `Teachers/GetTeacherClassLessons`

Adicionar `PlannedContent` ao `GetTeacherClassLessonsItemOut`, ao `Select` do service e aos
exemplos (uma aula com planejamento, outra sem).

### 4. Feature `Students/GetStudentClassLessons`

`Back/Features/Students/GetStudentClassLessons/` com Controller, Service e Out.

- `GET students/classes/{classId}/lessons`, `Authorize(Policies.GetStudentClassLessons)`
  (`UserType.Student`, em `Policies.Students.cs`).
- Mesmas checagens do `GetStudentClassService`: `ClassNotFound` e `StudentNotEnrolledInClass`
  (vínculo em `ClassStudents`, com qualquer status).
- Itens ordenados por `Number`: `Id`, `Number`, `Date`, `StartAt`, `EndAt`, `Status`,
  `PlannedContent`. Não inclui `PresentStudents`, que no out do professor traz a presença de todos
  os alunos.

### 5. Banco

O repositório não versiona migrations (não existe `ModelSnapshot`), e os testes criam o schema com
`EnsureCreatedAsync` (`Tests/Base/IntegrationTestBase.cs:56`). Nos testes a coluna aparece sem
nenhum passo extra. Para os ambientes, gerar a migration e o script conforme
`Back/Database/Migrations.md`; o SQL resultante é
`ALTER TABLE estud.class_lessons ADD planned_content text NULL;`.

## Testes de integração

Helpers novos:

- `TestsHttpClient.Teachers.cs`: `UpdateLessonPlan(int lessonId, string? plannedContent)`.
- `TestsHttpClient.Students.cs`: `GetStudentClassLessons(int classId)`.

`Tests/Features/Teachers/UpdateLessonPlan/UpdateLessonPlanIntegrationTests.cs`

- Authentication: sem login → 401.
- Authorization: diretor → 403; aluno → 403.
- Validation errors: aula inexistente; aula de outra instituição (`ClassLessonNotFound`); aula de
  outro professor (`TeacherNotAssignedToClass`); texto com 2001 caracteres
  (`InvalidClassLessonPlan`).
- Happy path, conferindo pelo `GetTeacherClassLessons`:
  - planeja uma aula futura (turma no último período, como em
    `Teachers_CreateLessonAttendance_Should_not_create_attendance_when_lesson_is_in_the_future`);
  - edita um planejamento existente;
  - texto vazio ou só com espaços limpa o planejamento (`null`);
  - planeja uma aula que já teve chamada, e ela continua `Finalized` com os mesmos presentes.

`Tests/Features/Students/GetStudentClassLessons/GetStudentClassLessonsIntegrationTests.cs`

- Authentication: sem login → 401.
- Authorization: diretor → 403; professor → 403.
- Validation errors: turma inexistente (`ClassNotFound`); aluno de outra turma
  (`StudentNotEnrolledInClass`).
- Happy path: aluno vê todas as aulas em ordem, com o planejamento que o professor gravou e `null`
  nas aulas sem planejamento.

## Front-end

### Tipos e utils

- `Web/app/types/classes.ts`: `plannedContent: string | null` no `ClassLessonItem`. Novos
  `StudentClassLessonItem` (sem `presentStudents`) e `GetStudentClassLessonsOut`.
- `Web/app/utils/classes.ts`: `formatClassLesson` passa a receber
  `Pick<ClassLessonItem, 'date' | 'startAt' | 'endAt'>`, para atender aos dois tipos.

### Professor

Novo `Web/app/components/classes/LessonPlanModal.vue`, no molde do `LessonAttendanceModal`:

- Props `lesson: ClassLessonItem | null`, `v-model:open`, emite `saved`. Fullscreen no mobile.
- Título "Planejamento · Aula N", com a data e o horário na descrição.
- `UForm` com zod `z.string().max(2000, 'Máximo de 2000 caracteres')`. O campo não é
  obrigatório, porque vazio limpa o planejamento.
- `UTextarea` com `autoresize` e contador de caracteres; ao abrir, carrega o `plannedContent`
  atual.
- `PUT teachers/lessons/{id}/plan`, toast de sucesso ou erro, fecha e emite `saved`.

`Web/app/components/classes/DetailTeacher.vue`, aba Aulas:

- Abaixo de "Aula N · data", o planejamento com `line-clamp-2` e `whitespace-pre-line`, ou "Sem
  planejamento" em `text-dimmed`.
- Botão "Planejar" / "Editar plano" (`i-lucide-notebook-pen`) ao lado de "Fazer chamada", sempre
  habilitado. `@saved` chama `refreshLessons()`.

### Aluno

`Web/app/components/classes/DetailStudent.vue`:

- Fetch de `students/classes/{id}/lessons`, junto dos outros.
- Seção "Aulas (N)" no fim da página: lista com "Aula N", data e horário, badge de status e o
  planejamento (texto completo, `whitespace-pre-line`) ou "Sem planejamento".

## Fora do escopo

- Conteúdo ministrado (`TaughtContent`) e retificação com justificativa: a parte do diário com
  validade legal.
- Bloquear a edição do planejamento com a turma finalizada.
- Avisar os alunos quando o planejamento muda.
- Plano de ensino da disciplina (ementa, objetivos, bibliografia, critérios de avaliação).
