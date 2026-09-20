namespace Estud.Back.Features.Students.CreateClassActivityWorkComment;

public class CreateClassActivityWorkCommentOut : IApiDto<CreateClassActivityWorkCommentOut>
{
    public int Id { get; set; }

    public static IEnumerable<(string, CreateClassActivityWorkCommentOut)> GetExamples() =>
    [
        ("Exemplo", new CreateClassActivityWorkCommentOut { Id = 1 }),
    ];
}
