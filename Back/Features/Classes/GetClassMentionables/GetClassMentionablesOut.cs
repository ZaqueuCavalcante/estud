namespace Estud.Back.Features.Classes.GetClassMentionables;

public class GetClassMentionablesOut : IApiDto<GetClassMentionablesOut>
{
    public List<GetClassMentionablesItemOut> Items { get; set; } = [];

    public static IEnumerable<(string, GetClassMentionablesOut)> GetExamples() =>
    [
        ("Exemplo", new GetClassMentionablesOut
        {
            Items =
            [
                new() { Id = 42, Name = "Ana Lima" },
                new() { Id = 57, Name = "Bruno Souza" },
            ],
        }),
    ];
}

public class GetClassMentionablesItemOut
{
    /// <summary>
    /// Id do usuário, não do aluno.
    /// </summary>
    public int Id { get; set; }

    public string Name { get; set; }
}
