namespace Estud.Back.Features.Students.GetStudentClassLesson;

public class GetStudentClassLessonOut : IApiDto<GetStudentClassLessonOut>
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

    public static IEnumerable<(string, GetStudentClassLessonOut)> GetExamples() =>
    [
        ("Exemplo", new GetStudentClassLessonOut
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
        }),
    ];
}
