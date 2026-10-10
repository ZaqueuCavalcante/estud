namespace Estud.Back.Features.Insights.GetStudentsAtRisk;

public class GetStudentsAtRiskOut : IApiDto<GetStudentsAtRiskOut>
{
    /// <summary>
    /// Quantidade de alunos matriculados em turmas do período
    /// </summary>
    public int TotalStudents { get; set; }

    /// <summary>
    /// Alunos em risco apenas por frequência
    /// </summary>
    public int OnlyFrequency { get; set; }

    /// <summary>
    /// Alunos em risco apenas por nota
    /// </summary>
    public int OnlyGrade { get; set; }

    /// <summary>
    /// Alunos em risco por frequência e por nota
    /// </summary>
    public int Both { get; set; }

    public static IEnumerable<(string, GetStudentsAtRiskOut)> GetExamples() =>
    [
        ("Exemplo", new GetStudentsAtRiskOut
        {
            TotalStudents = 1240,
            OnlyFrequency = 46,
            OnlyGrade = 91,
            Both = 41,
        }),
    ];
}
