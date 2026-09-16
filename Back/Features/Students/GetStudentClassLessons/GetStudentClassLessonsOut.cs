namespace Estud.Back.Features.Students.GetStudentClassLessons;

public class GetStudentClassLessonsOut : IApiDto<GetStudentClassLessonsOut>
{
    public List<GetStudentClassLessonsItemOut> Lessons { get; set; } = [];

    public static IEnumerable<(string, GetStudentClassLessonsOut)> GetExamples() =>
    [
        ("Exemplo", new GetStudentClassLessonsOut
        {
            Lessons =
            [
                new()
                {
                    Id = 1,
                    Number = 1,
                    Date = new DateOnly(2026, 3, 2),
                    StartAt = Hour.H19_00,
                    EndAt = Hour.H22_00,
                    Status = ClassLessonStatus.Finalized,
                    PlannedContent = "Introdução a grafos: definições e representação por matriz de adjacência.",
                },
                new()
                {
                    Id = 2,
                    Number = 2,
                    Date = new DateOnly(2026, 3, 9),
                    StartAt = Hour.H19_00,
                    EndAt = Hour.H22_00,
                    Status = ClassLessonStatus.Pending,
                    PlannedContent = null,
                },
            ],
        }),
    ];
}

public class GetStudentClassLessonsItemOut
{
    public int Id { get; set; }
    public int Number { get; set; }
    public DateOnly Date { get; set; }
    public Hour StartAt { get; set; }
    public Hour EndAt { get; set; }
    public ClassLessonStatus Status { get; set; }
    public string? PlannedContent { get; set; }
}
