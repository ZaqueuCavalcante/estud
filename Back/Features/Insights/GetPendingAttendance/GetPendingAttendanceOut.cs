namespace Estud.Back.Features.Insights.GetPendingAttendance;

public class GetPendingAttendanceOut : IApiDto<GetPendingAttendanceOut>
{
    /// <summary>
    /// Quantidade de aulas do período que já ocorreram
    /// </summary>
    public int PastLessons { get; set; }

    /// <summary>
    /// Quantidade de aulas que já ocorreram e ainda estão sem chamada
    /// </summary>
    public int PendingLessons { get; set; }

    /// <summary>
    /// Percentual das aulas que já ocorreram com chamada lançada (de 0% a 100%)
    /// </summary>
    public decimal UpToDate { get; set; }

    public List<GetPendingAttendanceClassOut> Classes { get; set; } = [];

    public static IEnumerable<(string, GetPendingAttendanceOut)> GetExamples() =>
    [
        ("Exemplo", new GetPendingAttendanceOut
        {
            PastLessons = 1380,
            PendingLessons = 23,
            UpToDate = 98.3M,
            Classes =
            [
                new() { Id = 3, Discipline = "Cálculo I", Teachers = ["Marina Albuquerque"], PendingLessons = 7, OldestPendingAt = new DateOnly(2026, 9, 15) },
                new() { Id = 7, Discipline = "Física Geral", Teachers = ["Paulo Henrique Lima"], PendingLessons = 4, OldestPendingAt = new DateOnly(2026, 9, 29) },
            ],
        }),
    ];
}

public class GetPendingAttendanceClassOut
{
    public int Id { get; set; }
    public string Discipline { get; set; }
    public List<string> Teachers { get; set; } = [];
    public int PendingLessons { get; set; }

    /// <summary>
    /// Data da aula mais antiga da turma que ainda está sem chamada
    /// </summary>
    public DateOnly OldestPendingAt { get; set; }
}
