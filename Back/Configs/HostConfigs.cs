namespace Estud.Back.Configs;

public static class HostConfigs
{
    public static void AddHostConfigs(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);

            if (context.Configuration.OpenTelemetry.Enabled)
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
