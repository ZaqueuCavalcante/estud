namespace Estud.Back.Features.Disciplines.AssignTeachersToDiscipline;

public class AssignTeachersToDisciplineIn : IApiDto<AssignTeachersToDisciplineIn>
{
    public List<int> Teachers { get; set; } = [];

    public static IEnumerable<(string, AssignTeachersToDisciplineIn)> GetExamples() =>
    [
        ("Exemplo", new() { Teachers = [1, 2, 3] }),
    ];
}
