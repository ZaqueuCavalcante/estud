namespace Estud.Back.Features.Students.GetStudentClassLessons;

public class GetStudentClassLessonsIn : IApiDto<GetStudentClassLessonsIn>
{
    public string? Search { get; set; }

    public static IEnumerable<(string, GetStudentClassLessonsIn)> GetExamples() =>
    [
        ("Exemplo", new GetStudentClassLessonsIn { Search = "grafos" }),
    ];
}
