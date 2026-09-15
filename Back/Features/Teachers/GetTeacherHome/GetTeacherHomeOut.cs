namespace Estud.Back.Features.Teachers.GetTeacherHome;

public class GetTeacherHomeOut : IApiDto<GetTeacherHomeOut>
{
    /// <summary>
    /// Turmas iniciadas que o professor leciona.
    /// </summary>
    public int ActiveClasses { get; set; }

    /// <summary>
    /// Alunos distintos nas turmas iniciadas do professor.
    /// </summary>
    public int Students { get; set; }

    /// <summary>
    /// Turmas não finalizadas do professor, com as iniciadas primeiro.
    /// </summary>
    public List<GetTeacherHomeClassOut> Classes { get; set; } = [];

    public static IEnumerable<(string, GetTeacherHomeOut)> GetExamples() =>
    [
        ("Exemplo", new GetTeacherHomeOut
        {
            ActiveClasses = 2,
            Students = 65,
            Classes =
            [
                new GetTeacherHomeClassOut
                {
                    Id = 10,
                    Discipline = "Banco de Dados",
                    Period = "2026.2",
                    Campus = "Campus Maceió",
                    Status = ClassStatus.Started,
                    Students = 34,
                    Lessons = 40,
                    FinishedLessons = 18,
                },
                new GetTeacherHomeClassOut
                {
                    Id = 12,
                    Discipline = "Estruturas de Dados",
                    Period = "2026.2",
                    Status = ClassStatus.Started,
                    Students = 31,
                    Lessons = 20,
                    FinishedLessons = 9,
                },
                new GetTeacherHomeClassOut
                {
                    Id = 15,
                    Discipline = "Engenharia de Software",
                    Period = "2027.1",
                    Campus = "Campus Maceió",
                    Status = ClassStatus.OnEnrollment,
                    Students = 12,
                },
            ],
        }),
    ];
}

public class GetTeacherHomeClassOut
{
    public int Id { get; set; }
    public string Discipline { get; set; }
    public string Period { get; set; }

    /// <summary>
    /// Nulo quando a turma não é presencial.
    /// </summary>
    public string? Campus { get; set; }
    public ClassStatus Status { get; set; }
    public int Students { get; set; }

    /// <summary>
    /// Total de aulas da turma. Zero enquanto a turma não é iniciada.
    /// </summary>
    public int Lessons { get; set; }
    public int FinishedLessons { get; set; }
}
