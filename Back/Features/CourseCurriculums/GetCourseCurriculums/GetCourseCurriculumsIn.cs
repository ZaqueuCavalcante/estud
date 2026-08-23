namespace Estud.Back.Features.CourseCurriculums.GetCourseCurriculums;

public class GetCourseCurriculumsIn : IApiDto<GetCourseCurriculumsIn>
{
    public string? Filter { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public static IEnumerable<(string, GetCourseCurriculumsIn)> GetExamples() =>
    [
        ("Exemplo", new() { }),
    ];
}
