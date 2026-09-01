using Estud.Back.Domain.Identity;

namespace Estud.Back.Features.Identity.CreateSsoConfiguration;

public class CreateSsoConfigurationOut : IApiDto<CreateSsoConfigurationOut>
{
    public Guid Id { get; set; }
    public string Domain { get; set; }
    public SsoDomainStatus DomainStatus { get; set; }
    public string VerificationToken { get; set; }

    /// <summary>
    /// Name of the DNS TXT record the institution must publish to prove it owns the domain.
    /// </summary>
    public string RecordName => $"{SsoAllowedDomain.RecordPrefix}.{Domain}";

    /// <summary>
    /// Value of the DNS TXT record the institution must publish to prove it owns the domain.
    /// </summary>
    public string RecordValue => $"{SsoAllowedDomain.RecordValuePrefix}{VerificationToken}";

    public static IEnumerable<(string, CreateSsoConfigurationOut)> GetExamples() =>
    [
        ("Exemplo", new CreateSsoConfigurationOut
        {
            Id = Guid.NewGuid(),
            Domain = "empresa.com.br",
            DomainStatus = SsoDomainStatus.Pending,
            VerificationToken = "9f2c8b1d4e6a7f30b5c9d2e1a4f60837",
        }),
    ];
}
