namespace Estud.Back.Features.Teachers.CreateTeacher;

public class CreateTeacherIn : IApiDto<CreateTeacherIn>
{
    public string Name { get; set; }
    public string Email { get; set; }

    public static IEnumerable<(string, CreateTeacherIn)> GetExamples() =>
    [
        ("Exemplo", new() { Name = "Richard Feynman", Email = "feynman@estud.com" }),
    ];
}
