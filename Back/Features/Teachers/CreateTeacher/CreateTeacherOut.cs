namespace Estud.Back.Features.Teachers.CreateTeacher;

public class CreateTeacherOut : IApiDto<CreateTeacherOut>
{
    public int Id { get; set; }
    public string Email { get; set; }

    public static IEnumerable<(string, CreateTeacherOut)> GetExamples() =>
    [
        ("Paulo", new CreateTeacherOut { Id = 1 })
    ];
}
