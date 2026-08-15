namespace Estud.Back.Features.CourseCurriculums.GetCourseCurriculumDetails;

public class GetCourseCurriculumDetailsOut : IApiDto<GetCourseCurriculumDetailsOut>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CourseId { get; set; }
    public string Course { get; set; }
    public string CourseType { get; set; }

    /// <summary>
    /// Maior período entre as disciplinas da grade
    /// </summary>
    public int Periods { get; set; }

    public int TotalCredits { get; set; }
    public int TotalWorkload { get; set; }

    /// <summary>
    /// Quantidade de alunos com vínculo ativo em alguma oferta que usa esta grade
    /// </summary>
    public int Students { get; set; }

    public List<GetCourseCurriculumDetailsDisciplineOut> Disciplines { get; set; } = [];
    public List<GetCourseCurriculumDetailsOfferingOut> Offerings { get; set; } = [];

    public static IEnumerable<(string, GetCourseCurriculumDetailsOut)> GetExamples() =>
    [
        ("Exemplo", new GetCourseCurriculumDetailsOut
        {
            Id = 1,
            Name = "Grade ADS 2024",
            CourseId = 1,
            Course = "Análise e Desenvolvimento de Sistemas",
            CourseType = "Tecnólogo",
            Periods = 6,
            TotalCredits = 4,
            TotalWorkload = 60,
            Students = 32,
            Disciplines =
            [
                new GetCourseCurriculumDetailsDisciplineOut
                {
                    Id = 1,
                    Name = "Cálculo I",
                    Code = "A1B2C3D4",
                    Period = 1,
                    Credits = 4,
                    Workload = 60,
                },
            ],
            Offerings =
            [
                new GetCourseCurriculumDetailsOfferingOut
                {
                    Id = 1,
                    Campus = "Campus Maceió",
                    Period = "2026.1",
                    Session = CourseSession.Evening,
                    Students = 32,
                },
            ],
        }),
    ];
}

public class GetCourseCurriculumDetailsDisciplineOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public byte Period { get; set; }
    public byte Credits { get; set; }
    public ushort Workload { get; set; }
}

public class GetCourseCurriculumDetailsOfferingOut
{
    public int Id { get; set; }
    public string Campus { get; set; }
    public string Period { get; set; }
    public CourseSession Session { get; set; }
    public int Students { get; set; }
}
