namespace Estud.Back.Features.Users.GetUserAccount;

public class GetUserAccountOut : IApiDto<GetUserAccountOut>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int InstitutionId { get; set; }
    public string Institution { get; set; }
    public string Role { get; set; }
    public UserType UserType { get; set; }
    public List<int> Permissions { get; set; } = [];
    public string ProfilePhoto { get; set; }

    /// <summary>
    /// Curso, caso seja um Aluno.
    /// </summary>
    public string? Course { get; set; }

    /// <summary>
    /// Indica se o usuário é adm do Estud.
    /// </summary>
    public bool Adm { get; set; }

    public static IEnumerable<(string, GetUserAccountOut)> GetExamples() =>
    [
        ("Edson Gomes",
        new GetUserAccountOut()
        {
            Id = 1,
            Name = "Edson Gomes",
            Email = "edson.gomes@estud.com.br",
            Institution = "UFPE",
        }),
        ("Maria Júlia",
        new GetUserAccountOut()
        {
            Id = 2,
            Name = "Maria Júlia",
            Email = "maria.julia@estud.com.br",
            Institution = "Faculdade Nova Roma",
        }),
    ];
}
