namespace Estud.Back.Features.CourseOfferings.GetCourseOfferingDetails;

public class GetCourseOfferingDetailsOut : IApiDto<GetCourseOfferingDetailsOut>
{
    public int Id { get; set; }
    public int CampusId { get; set; }
    public string Campus { get; set; }
    public int CourseId { get; set; }
    public string Course { get; set; }
    public string CourseType { get; set; }
    public int CourseCurriculumId { get; set; }
    public string Curriculum { get; set; }
    public string Period { get; set; }
    public DateOnly PeriodStartAt { get; set; }
    public DateOnly PeriodEndAt { get; set; }
    public CourseSession Session { get; set; }

    /// <summary>
    /// Quantidade de disciplinas da grade curricular usada pela oferta
    /// </summary>
    public int Disciplines { get; set; }

    /// <summary>
    /// Alunos com vínculo ativo na oferta
    /// </summary>
    public List<GetCourseOfferingDetailsStudentOut> Students { get; set; } = [];

    public static IEnumerable<(string, GetCourseOfferingDetailsOut)> GetExamples() =>
    [
        ("Exemplo", new GetCourseOfferingDetailsOut
        {
            Id = 1,
            CampusId = 1,
            Campus = "Campus Maceió",
            CourseId = 1,
            Course = "Análise e Desenvolvimento de Sistemas",
            CourseType = "Tecnólogo",
            CourseCurriculumId = 1,
            Curriculum = "Grade ADS 2024",
            Period = "2026.1",
            PeriodStartAt = new DateOnly(2026, 2, 2),
            PeriodEndAt = new DateOnly(2026, 6, 30),
            Session = CourseSession.Evening,
            Disciplines = 30,
            Students =
            [
                new GetCourseOfferingDetailsStudentOut
                {
                    Id = 1,
                    Name = "Maria Souza",
                    EnrollmentCode = "20251A2B3C4D",
                    Status = StudentStatus.Enrolled,
                    EnrolledAt = new DateTime(2026, 2, 2, 13, 30, 0, DateTimeKind.Utc),
                },
            ],
        }),
    ];
}

public class GetCourseOfferingDetailsStudentOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string EnrollmentCode { get; set; }
    public StudentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
}
