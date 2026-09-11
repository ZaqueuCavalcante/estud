namespace Estud.Back.Features.Identity.VerifySsoDomain;

public class VerifySsoDomainIn : IApiDto<VerifySsoDomainIn>
{
    public string Domain { get; set; }

    public static IEnumerable<(string Name, VerifySsoDomainIn Value)> GetExamples() =>
    [
        ("Exemplo", new VerifySsoDomainIn { Domain = "universidade.edu.br" }),
    ];
}
