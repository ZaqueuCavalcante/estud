using Estud.Back.Domain.Identity;

namespace Estud.Back.Features.Identity.GetSsoConfiguration;

public class GetSsoConfigurationOut : IApiDto<GetSsoConfigurationOut>
{
    public Guid Id { get; set; }
    public SsoProviderType ProviderType { get; set; }
    public string Authority { get; set; }
    public string ClientId { get; set; }
    public bool IsActive { get; set; }
    public bool RequireSso { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SsoDomainOut> Domains { get; set; } = [];

    public static IEnumerable<(string, GetSsoConfigurationOut)> GetExamples() =>
    [
        ("Exemplo", new GetSsoConfigurationOut
        {
            Id = Guid.NewGuid(),
            ProviderType = SsoProviderType.AzureAd,
            Authority = "https://login.microsoftonline.com/tenant-id/v2.0",
            ClientId = "00000000-0000-0000-0000-000000000000",
            IsActive = true,
            RequireSso = false,
            CreatedAt = DateTime.UtcNow,
            Domains =
            [
                new SsoDomainOut
                {
                    Domain = "empresa.com.br",
                    Status = SsoDomainStatus.Pending,
                    VerificationToken = "9f2c8b1d4e6a7f30b5c9d2e1a4f60837",
                },
            ],
        }),
    ];
}

public class SsoDomainOut
{
    public string Domain { get; set; }
    public SsoDomainStatus Status { get; set; }
    public string VerificationToken { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public string? LastError { get; set; }

    /// <summary>
    /// Name of the DNS TXT record the institution must publish to prove it owns the domain.
    /// </summary>
    public string RecordName => $"{SsoAllowedDomain.RecordPrefix}.{Domain}";

    /// <summary>
    /// Value of the DNS TXT record the institution must publish to prove it owns the domain.
    /// </summary>
    public string RecordValue => $"{SsoAllowedDomain.RecordValuePrefix}{VerificationToken}";
}
