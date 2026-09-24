namespace Estud.Back.Settings;

public class EmailSettings : SettingsBase
{
    public string ApiUrl { get; set; }
    public string ApiKey { get; set; }
    public string FrontUrl { get; set; }

    public EmailSettings(IConfiguration configuration)
    {
        configuration.GetSection("Email").Bind(this);

        RequireNonEmpty(ApiUrl);
        RequireNonEmpty(ApiKey);
        RequireNonEmpty(FrontUrl);
    }
}
