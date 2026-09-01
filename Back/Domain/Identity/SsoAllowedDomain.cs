using System.Security.Cryptography;

namespace Estud.Back.Domain.Identity;

/// <summary>
/// Allowed email domain for SSO configuration. <br/>
/// Each domain can only be linked to one organization's SSO config, and only
/// routes logins after its ownership is proven through a DNS TXT record.
/// </summary>
public class SsoAllowedDomain
{
    public const string RecordPrefix = "_estud-verification";
    public const string RecordValuePrefix = "estud-domain-verification=";

    /// <summary>
    /// The email domain (e.g., "empresa.com"). <br/>
    /// Must be unique across all SSO configurations.
    /// </summary>
    public string Domain { get; set; }

    public int SsoConfigurationId { get; set; }
    public SsoConfiguration? Configuration { get; set; }

    public SsoDomainStatus Status { get; set; }

    /// <summary>
    /// Random value the institution must publish in the DNS TXT record to prove it owns the domain.
    /// </summary>
    public string VerificationToken { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public string? LastError { get; set; }

    public SsoAllowedDomain() { }

    public SsoAllowedDomain(string domain)
    {
        Domain = domain.ToLowerInvariant();
        Status = SsoDomainStatus.Pending;
        VerificationToken = GenerateVerificationToken();
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsVerified => Status == SsoDomainStatus.Verified;

    public string RecordName => $"{RecordPrefix}.{Domain}";
    public string RecordValue => $"{RecordValuePrefix}{VerificationToken}";

    public bool Matches(IEnumerable<string> txtRecords)
    {
        return txtRecords.Any(r => r.Trim().Equals(RecordValue, StringComparison.OrdinalIgnoreCase));
    }

    public void Verify()
    {
        Status = SsoDomainStatus.Verified;
        VerifiedAt = DateTime.UtcNow;
        LastCheckedAt = VerifiedAt;
        LastError = null;
    }

    public void Fail(string error)
    {
        // A domain that was already proven becomes Expired, not Failed: the distinction is what
        // tells support "this used to work and stopped" apart from "this was never set up".
        Status = Status is SsoDomainStatus.Verified or SsoDomainStatus.Expired
            ? SsoDomainStatus.Expired
            : SsoDomainStatus.Failed;

        VerifiedAt = null;
        LastCheckedAt = DateTime.UtcNow;
        LastError = error;
    }

    private static string GenerateVerificationToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }
}
