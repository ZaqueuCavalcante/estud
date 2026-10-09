namespace Estud.Back.Features.Insights.GetLowestAttendanceClasses;

public class GetLowestAttendanceClassesIn : IApiDto<GetLowestAttendanceClassesIn>
{
    public int PeriodId { get; set; }

    public static IEnumerable<(string, GetLowestAttendanceClassesIn)> GetExamples() =>
    [
        ("Exemplo", new GetLowestAttendanceClassesIn { PeriodId = 1 }),
    ];
}
