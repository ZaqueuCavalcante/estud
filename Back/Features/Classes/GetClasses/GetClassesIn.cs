namespace Estud.Back.Features.Classes.GetClasses;

public class GetClassesIn : IApiDto<GetClassesIn>
{
    public string? Filter { get; set; }
    public int? PeriodId { get; set; }
    public ClassStatus? Status { get; set; }
    public bool? AllLessonsFinished { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public static IEnumerable<(string, GetClassesIn)> GetExamples() =>
    [
        ("Exemplo", new() { }),
    ];
}
