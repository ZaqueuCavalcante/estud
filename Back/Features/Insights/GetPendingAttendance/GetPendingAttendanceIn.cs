namespace Estud.Back.Features.Insights.GetPendingAttendance;

public class GetPendingAttendanceIn : IApiDto<GetPendingAttendanceIn>
{
    public int PeriodId { get; set; }

    public static IEnumerable<(string, GetPendingAttendanceIn)> GetExamples() =>
    [
        ("Exemplo", new GetPendingAttendanceIn { PeriodId = 1 }),
    ];
}
