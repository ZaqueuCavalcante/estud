namespace Estud.Back.Features.Insights.GetStudentsAtRisk;

public class GetStudentsAtRiskIn : IApiDto<GetStudentsAtRiskIn>
{
    public int PeriodId { get; set; }

    public static IEnumerable<(string, GetStudentsAtRiskIn)> GetExamples() =>
    [
        ("Exemplo", new GetStudentsAtRiskIn { PeriodId = 1 }),
    ];
}
