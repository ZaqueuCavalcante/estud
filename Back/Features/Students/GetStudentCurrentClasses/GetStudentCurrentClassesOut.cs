namespace Estud.Back.Features.Students.GetStudentCurrentClasses;

public class GetStudentCurrentClassesOut : IApiDto<GetStudentCurrentClassesOut>
{
    public List<GetStudentCurrentClassesItemOut> Classes { get; set; } = [];

    public static IEnumerable<(string Name, GetStudentCurrentClassesOut Value)> GetExamples() =>
    [
        new() { Name = "Exemplo", Value = new()
        {
            Classes =
            [
                new() { Id = 1, Name = "Cálculo I" },
                new() { Id = 2, Name = "Geometria Analítica" },
            ]
        }}
    ];
}

public class GetStudentCurrentClassesItemOut
{
    public int Id { get; set; }
    public string Name { get; set; }
}
