namespace Estud.Back.Features.Insights.GetLowestGradeClasses;

public class GetLowestGradeClassesOut : IApiDto<GetLowestGradeClassesOut>
{
    public List<GetLowestGradeClassesItemOut> Classes { get; set; } = [];

    public static IEnumerable<(string, GetLowestGradeClassesOut)> GetExamples() =>
    [
        ("Exemplo", new GetLowestGradeClassesOut
        {
            Classes =
            [
                new() { Id = 3, Discipline = "Cálculo I", Average = 4.8M },
                new() { Id = 7, Discipline = "Física Geral", Average = 5.3M },
            ],
        }),
    ];
}

public class GetLowestGradeClassesItemOut
{
    public int Id { get; set; }
    public string Discipline { get; set; }

    /// <summary>
    /// Nota média da turma (de 0 a 10)
    /// </summary>
    public decimal Average { get; set; }
}
