namespace Estud.Back.Features.Courses.UpdateCourse;

public class UpdateCourseIn : IApiDto<UpdateCourseIn>
{
    public string Name { get; set; }
    public CourseType? Type { get; set; }

    public static IEnumerable<(string, UpdateCourseIn)> GetExamples() =>
    [
        ("Exemplo", new() { Name = "Análise e Desenvolvimento de Sistemas", Type = CourseType.Tecnologo }),
    ];
}
