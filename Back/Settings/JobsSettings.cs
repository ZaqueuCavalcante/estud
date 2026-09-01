namespace Estud.Back.Settings;

public class JobsSettings : SettingsBase
{
    public int CommandsPollingIntervalInSeconds { get; set; } = 60;
    public int DomainEventsPollingIntervalInSeconds { get; set; } = 60;
    public int SsoDomainsPollingIntervalInSeconds { get; set; } = 3600;

    public JobsSettings(IConfiguration configuration)
    {
        configuration.GetSection("Jobs").Bind(this);

        RequirePositive(CommandsPollingIntervalInSeconds);
        RequirePositive(DomainEventsPollingIntervalInSeconds);
        RequirePositive(SsoDomainsPollingIntervalInSeconds);
    }
}

public static class JobsSettingsExtensions
{
    extension(IConfiguration configuration)
    {
        public JobsSettings Jobs => new(configuration);
    }
}
