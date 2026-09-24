using Quartz;
using Estud.Back.Emails;
using System.Diagnostics;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Estud.Tests.Integration.Clients;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estud.Tests.Base;

extern alias Back;

public class BackFactory : WebApplicationFactory<Back::Program>
{
    // Só guarda spans de traces abertos pelos testes; senão a suíte inteira fica em memória.
    public TelemetryCollection<MetricSnapshot> Metrics { get; } = new();
    public TelemetryCollection<Activity> Spans { get; } = new(BackFactoryTelemetry.IsTestTrace);

    public BackFactory() : base()
    {
        UseKestrel(o => o.ListenLocalhost(5100));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnvironmentExtensions.SetAsTesting();

        builder.UseTestServer();

        builder.ConfigureAppConfiguration(config =>
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Testing.json");

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath)
                .Build();

            config.AddConfiguration(configuration);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, RequestsCounterStartupFilter>();

            services.ConfigureOpenTelemetryTracerProvider(tracing => tracing
                .AddSource(BackFactoryTelemetry.TestsActivitySource.Name)
                .AddInMemoryExporter(Spans));

            services.ConfigureOpenTelemetryMeterProvider(metrics => metrics
                .AddInMemoryExporter(Metrics));
        });
    }

    public TestsHttpClient GetTestsClient(bool followRedirects = true)
    {
        // WebApplicationFactoryClientOptions.AllowAutoRedirect só desliga o RedirectHandler dela;
        // sobre Kestrel quem segue os redirects é o HttpClientHandler interno, que ela não expõe.
        var client = followRedirects ? CreateClient()
            : new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = ClientOptions.BaseAddress };

        client.Timeout = TimeSpan.FromHours(1);
        return new TestsHttpClient(client);
    }

    public EstudDbContext GetDbContext([CallerMemberName] string operation = "")
    {
        var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<EstudDbContext>();
        ctx.Enrich($"Tests.{operation}");
        return ctx;
    }

    public ISchedulerFactory GetSchedulerFactory()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
    }

    public FakeEmailsService GetFakeEmailsService()
    {
        return (FakeEmailsService)Services.GetRequiredService<IEmailsService>();
    }
}
