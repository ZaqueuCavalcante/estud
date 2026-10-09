namespace Estud.Back.Features.Insights.GetAttendance;

public class GetAttendanceIn : IApiDto<GetAttendanceIn>
{
    public int PeriodId { get; set; }

    public static IEnumerable<(string, GetAttendanceIn)> GetExamples() =>
    [
        ("Exemplo", new GetAttendanceIn { PeriodId = 1 }),
    ];
}
