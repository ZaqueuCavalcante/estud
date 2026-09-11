namespace Estud.Back.Settings;

public class DnsSettings : SettingsBase
{
    /// <summary>
    /// DNS-over-HTTPS resolver with the JSON API.
    /// </summary>
    public string ResolverUrl { get; set; }

    public DnsSettings(IConfiguration configuration)
    {
        configuration.GetSection("Dns").Bind(this);

        RequireNonEmpty(ResolverUrl);
    }
}

public static class DnsSettingsExtensions
{
    extension(IConfiguration configuration)
    {
        public DnsSettings Dns => new(configuration);
    }
}
