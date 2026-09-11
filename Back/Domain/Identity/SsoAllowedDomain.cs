using System.Security.Cryptography;

namespace Estud.Back.Domain.Identity;

/// <summary>
/// Allowed email domain for SSO configuration. <br/>
/// A domain only routes logins after its ownership is proven via a DNS TXT record.
/// </summary>
public class SsoAllowedDomain
{
    public int Id { get; set; }

    /// <summary>
    /// The email domain (e.g., "empresa.com"). <br/>
    /// Many institutions may claim it, but only one can have it verified.
    /// </summary>
    public string Domain { get; set; }

    public int SsoConfigurationId { get; set; }
    public SsoConfiguration? SsoConfiguration { get; set; }

    public SsoDomainStatus Status { get; set; }
    public string VerificationToken { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public string TxtRecordName => $"{SsoConstants.TxtRecordPrefix}.{Domain}";
    public string TxtRecordValue => $"{SsoConstants.TxtValuePrefix}{VerificationToken}";

    public SsoAllowedDomain() { }

    public SsoAllowedDomain(string domain)
    {
        Domain = domain.ToLowerInvariant();
        Status = SsoDomainStatus.Pending;
        VerificationToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    }

    public void MarkAsVerified()
    {
        Status = SsoDomainStatus.Verified;
        VerifiedAt = DateTime.UtcNow;
    }
}
