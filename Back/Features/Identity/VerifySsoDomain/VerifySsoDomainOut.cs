namespace Estud.Back.Features.Identity.VerifySsoDomain;

public class VerifySsoDomainOut : IApiDto<VerifySsoDomainOut>
{
    public string Domain { get; set; }
    public SsoDomainStatus Status { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public static IEnumerable<(string, VerifySsoDomainOut)> GetExamples() =>
    [
        ("Exemplo", new VerifySsoDomainOut
        {
            Domain = "empresa.com.br",
            Status = SsoDomainStatus.Verified,
            VerifiedAt = DateTime.UtcNow,
        }),
    ];
}
