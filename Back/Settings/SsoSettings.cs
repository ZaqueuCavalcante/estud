namespace Estud.Back.Settings;

public class SsoSettings : SettingsBase
{
    /// <summary>
    /// DNS-over-HTTPS resolver used to read the domain verification TXT records.
    /// </summary>
    public string DnsResolverUrl { get; set; } = "https://cloudflare-dns.com/dns-query";

    public int DnsTimeoutInSeconds { get; set; } = 10;

    /// <summary>
    /// How long a verified domain stays trusted before the background job re-checks its TXT record.
    /// </summary>
    public int DomainRecheckIntervalInHours { get; set; } = 168;

    public SsoSettings(IConfiguration configuration)
    {
        configuration.GetSection("Sso").Bind(this);

        RequireNonEmpty(DnsResolverUrl);
        RequirePositive(DnsTimeoutInSeconds);
        RequirePositive(DomainRecheckIntervalInHours);

        DnsResolverUrl = DnsResolverUrl.TrimEnd('/');
    }
}

public static class SsoSettingsExtensions
{
    extension(IConfiguration configuration)
    {
        public SsoSettings Sso => new(configuration);
    }
}
