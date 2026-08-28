namespace Estud.Back.Features.Courses.AssignDisciplinesToCourse;

public class AssignDisciplinesToCourseIn : IApiDto<AssignDisciplinesToCourseIn>
{
    public List<int> Disciplines { get; set; } = [];

    public static IEnumerable<(string, AssignDisciplinesToCourseIn)> GetExamples() =>
    [
        ("Exemplo", new() { Disciplines = [1, 2, 3] }),
    ];
}
