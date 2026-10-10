namespace Estud.Back.Features.Insights.GetLowestGradeClasses;

public class GetLowestGradeClassesIn : IApiDto<GetLowestGradeClassesIn>
{
    public int PeriodId { get; set; }

    public static IEnumerable<(string, GetLowestGradeClassesIn)> GetExamples() =>
    [
        ("Exemplo", new GetLowestGradeClassesIn { PeriodId = 1 }),
    ];
}
