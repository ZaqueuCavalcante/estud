namespace Estud.Back.Configs;

public static class HostConfigs
{
    public static void AddHostConfigs(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);

            var settings = context.Configuration.OpenTelemetry;

            if (settings.Enabled && settings.OtlpExporterEnabled)
            {
                config.WriteTo.OpenTelemetry(options =>
                {
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = OpenTelemetryConfigs.ServiceName,
                    };
                });
            }
        });
    }
}
