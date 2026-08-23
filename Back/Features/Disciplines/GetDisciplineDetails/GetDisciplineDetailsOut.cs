namespace Estud.Back.Features.Disciplines.GetDisciplineDetails;

public class GetDisciplineDetailsOut : IApiDto<GetDisciplineDetailsOut>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public List<GetDisciplineDetailsCourseOut> Courses { get; set; } = [];
    public List<GetDisciplineDetailsTeacherOut> Teachers { get; set; } = [];
    public List<GetDisciplineDetailsClassOut> Classes { get; set; } = [];

    public static IEnumerable<(string, GetDisciplineDetailsOut)> GetExamples() =>
    [
        ("Exemplo", new GetDisciplineDetailsOut
        {
            Id = 3,
            Name = "Banco de Dados",
            Code = "ABC12345",
            Courses = [new GetDisciplineDetailsCourseOut { Id = 1, Name = "ADS" }],
            Teachers = [new GetDisciplineDetailsTeacherOut { Id = 14, Name = "Ana Lima" }],
            Classes =
            [
                new GetDisciplineDetailsClassOut
                {
                    Id = 1,
                    Period = "2026.1",
                    Campus = "Campus Maceió",
                    Vacancies = 40,
                    Students = 32,
                    Workload = 60,
                    Status = ClassStatus.Started,
                },
            ],
        }),
    ];
}

public class GetDisciplineDetailsCourseOut
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class GetDisciplineDetailsTeacherOut
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class GetDisciplineDetailsClassOut
{
    public int Id { get; set; }
    public string Period { get; set; }

    /// <summary>
    /// Nulo quando a turma não é presencial
    /// </summary>
    public string? Campus { get; set; }

    public int Vacancies { get; set; }

    /// <summary>
    /// Quantidade de alunos matriculados na turma
    /// </summary>
    public int Students { get; set; }

    public int Workload { get; set; }
    public ClassStatus Status { get; set; }
}
