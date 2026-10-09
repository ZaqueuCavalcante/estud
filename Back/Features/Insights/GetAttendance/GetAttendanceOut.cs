namespace Estud.Back.Features.Insights.GetAttendance;

public class GetAttendanceOut : IApiDto<GetAttendanceOut>
{
    /// <summary>
    /// Frequência média das turmas no período (de 0% a 100%)
    /// </summary>
    public decimal Average { get; set; }

    /// <summary>
    /// Quantidade de turmas com frequência abaixo da mínima da instituição
    /// </summary>
    public int BelowLimitClasses { get; set; }

    /// <summary>
    /// Quantidade de turmas com frequência igual ou acima da mínima da instituição
    /// </summary>
    public int AboveLimitClasses { get; set; }

    public List<GetAttendanceDayOut> Days { get; set; } = [];

    public static IEnumerable<(string, GetAttendanceOut)> GetExamples() =>
    [
        ("Exemplo", new GetAttendanceOut
        {
            Average = 81.3M,
            BelowLimitClasses = 4,
            AboveLimitClasses = 21,
            Days =
            [
                new() { Date = new DateOnly(2026, 3, 2), Attendance = 85.0M },
                new() { Date = new DateOnly(2026, 3, 3), Attendance = 77.5M },
            ],
        }),
    ];
}

public class GetAttendanceDayOut
{
    public DateOnly Date { get; set; }

    /// <summary>
    /// Frequência média das turmas no dia (de 0% a 100%)
    /// </summary>
    public decimal Attendance { get; set; }
}
