namespace Estud.Back.Features.Parents.CreateParent;

public class CreateParentOut : IApiDto<CreateParentOut>
{
    public int Id { get; set; }
    public string Email { get; set; }

    public static IEnumerable<(string, CreateParentOut)> GetExamples() =>
    [
        ("Exemplo", new() { Id = 1 }),
    ];
}
