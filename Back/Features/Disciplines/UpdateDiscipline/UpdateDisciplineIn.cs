namespace Estud.Back.Features.Disciplines.UpdateDiscipline;

public class UpdateDisciplineIn : IApiDto<UpdateDisciplineIn>
{
    public string Name { get; set; }

    public static IEnumerable<(string, UpdateDisciplineIn)> GetExamples() =>
    [
        ("Exemplo", new() { Name = "Cálculo I" }),
    ];
}
