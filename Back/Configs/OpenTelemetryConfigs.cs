using Npgsql;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Estud.Back.Configs;

public static class OpenTelemetryConfigs
{
    public const string ServiceName = nameof(Back);
    public const string CommandsProcessing = nameof(CommandsProcessing);
    public const string DomainEventsProcessing = nameof(DomainEventsProcessing);

    public static void AddOpenTelemetryConfigs(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration.OpenTelemetry;

        if (!settings.Enabled) return;

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddNpgsqlInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                metrics
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel");

                if (settings.OtlpExporterEnabled) metrics.AddOtlpExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddNpgsql()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddSource(CommandsProcessing, DomainEventsProcessing);

                tracing.SetSampler(new TraceIdRatioBasedSampler(settings.TracingSamplingRatio));

                if (settings.OtlpExporterEnabled) tracing.AddOtlpExporter();
            });
    }
}
