namespace Estud.Back.Settings;

public class OpenTelemetrySettings
{
    public bool Enabled { get; set; }
    public double TracingSamplingRatio { get; set; }
    public bool OtlpExporterEnabled { get; set; } = true;

    public OpenTelemetrySettings(IConfiguration configuration)
    {
        configuration.GetSection("OpenTelemetry").Bind(this);
    }
}

public static class OpenTelemetrySettingsExtensions
{
    extension(IConfiguration configuration)
    {
        public OpenTelemetrySettings OpenTelemetry => new(configuration);
    }
}
