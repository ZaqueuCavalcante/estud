namespace Estud.Back.Features.Disciplines.GetDisciplinePotentialTeachers;

public class GetDisciplinePotentialTeachersOut : IApiDto<GetDisciplinePotentialTeachersOut>
{
    public List<GetDisciplinePotentialTeacherItemOut> Items { get; set; } = [];

    public static IEnumerable<(string, GetDisciplinePotentialTeachersOut)> GetExamples() =>
    [
        ("Exemplo", new() { Items = [new() { Id = 1, Name = "Ana Souza" }] }),
    ];
}

public class GetDisciplinePotentialTeacherItemOut
{
    public int Id { get; set; }
    public string Name { get; set; }
}
