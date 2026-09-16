namespace Estud.Back.Features.Teachers.GetTeacherClassLesson;

public class GetTeacherClassLessonOut : IApiDto<GetTeacherClassLessonOut>
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string Discipline { get; set; }
    public int Number { get; set; }
    public DateOnly Date { get; set; }
    public Hour StartAt { get; set; }
    public Hour EndAt { get; set; }
    public ClassLessonStatus Status { get; set; }
    public string? PlannedContent { get; set; }

    /// <summary>
    /// Alunos matriculados na turma, com a presença registrada na chamada
    /// </summary>
    public List<GetTeacherClassLessonStudentOut> Students { get; set; } = [];

    public static IEnumerable<(string, GetTeacherClassLessonOut)> GetExamples() =>
    [
        ("Exemplo", new GetTeacherClassLessonOut
        {
            Id = 1,
            ClassId = 1,
            Discipline = "Estrutura de Dados",
            Number = 1,
            Date = new DateOnly(2026, 3, 2),
            StartAt = Hour.H19_00,
            EndAt = Hour.H22_00,
            Status = ClassLessonStatus.Finalized,
            PlannedContent = "## Grafos\n\n- Definições\n- Representação por matriz de adjacência",
            Students =
            [
                new() { Id = 1, Name = "Maria Souza", Present = true },
                new() { Id = 2, Name = "Chico Ferreira", Present = false },
            ],
        }),
    ];
}

public class GetTeacherClassLessonStudentOut
{
    public int Id { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Indica se o aluno foi marcado como presente na chamada da aula
    /// </summary>
    public bool Present { get; set; }
}
