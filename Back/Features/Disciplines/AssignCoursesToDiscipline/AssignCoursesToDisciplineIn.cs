namespace Estud.Back.Features.Disciplines.AssignCoursesToDiscipline;

public class AssignCoursesToDisciplineIn : IApiDto<AssignCoursesToDisciplineIn>
{
    public List<int> Courses { get; set; } = [];

    public static IEnumerable<(string, AssignCoursesToDisciplineIn)> GetExamples() =>
    [
        ("Exemplo", new() { Courses = [1, 2, 3] }),
    ];
}
