namespace Estud.Back.Features.Insights.GetLowestAttendanceClasses;

public class GetLowestAttendanceClassesOut : IApiDto<GetLowestAttendanceClassesOut>
{
    public List<GetLowestAttendanceClassesItemOut> Classes { get; set; } = [];

    public static IEnumerable<(string, GetLowestAttendanceClassesOut)> GetExamples() =>
    [
        ("Exemplo", new GetLowestAttendanceClassesOut
        {
            Classes =
            [
                new() { Id = 3, Discipline = "Cálculo I", Attendance = 58.4M },
                new() { Id = 7, Discipline = "Física Geral", Attendance = 63.1M },
            ],
        }),
    ];
}

public class GetLowestAttendanceClassesItemOut
{
    public int Id { get; set; }
    public string Discipline { get; set; }

    /// <summary>
    /// Frequência média da turma (de 0% a 100%)
    /// </summary>
    public decimal Attendance { get; set; }
}
