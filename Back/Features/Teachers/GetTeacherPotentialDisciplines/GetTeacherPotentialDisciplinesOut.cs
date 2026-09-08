namespace Estud.Back.Features.Teachers.GetTeacherPotentialDisciplines;

public class GetTeacherPotentialDisciplinesOut : IApiDto<GetTeacherPotentialDisciplinesOut>
{
    public List<GetTeacherPotentialDisciplineItemOut> Items { get; set; } = [];

    public static IEnumerable<(string, GetTeacherPotentialDisciplinesOut)> GetExamples() =>
    [
        ("Exemplo", new() { Items = [new() { Id = 1, Name = "Cálculo I", Code = "A1B2C3D4" }] }),
    ];
}

public class GetTeacherPotentialDisciplineItemOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
}
