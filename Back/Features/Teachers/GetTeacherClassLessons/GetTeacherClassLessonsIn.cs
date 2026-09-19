namespace Estud.Back.Features.Teachers.GetTeacherClassLessons;

public class GetTeacherClassLessonsIn : IApiDto<GetTeacherClassLessonsIn>
{
    public string? Search { get; set; }

    public static IEnumerable<(string, GetTeacherClassLessonsIn)> GetExamples() =>
    [
        ("Exemplo", new GetTeacherClassLessonsIn { Search = "grafos" }),
    ];
}
