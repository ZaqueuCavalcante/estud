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
    public List<GetSsoConfigurationDomainOut> Domains { get; set; } = [];

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
                new GetSsoConfigurationDomainOut
                {
                    Domain = "universidade.edu.br",
                    Status = SsoDomainStatus.Pending,
                    VerifiedAt = null,
                    TxtRecordName = "_estud-challenge.universidade.edu.br",
                    TxtRecordValue = "estud-domain-verification=3f9a0c1e7b2d4a6f8e1c3b5d7f9a2c4e",
                },
            ],
        }),
    ];
}

public class GetSsoConfigurationDomainOut
{
    public string Domain { get; set; }
    public SsoDomainStatus Status { get; set; }
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Name of the DNS TXT record the institution must publish to prove it owns the domain.
    /// </summary>
    public string TxtRecordName { get; set; }

    /// <summary>
    /// Value of the DNS TXT record the institution must publish to prove it owns the domain.
    /// </summary>
    public string TxtRecordValue { get; set; }
}
