using Quartz;
using System.Diagnostics;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Estud.Tests.Integration.Clients;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace Estud.Tests.Base;

extern alias Back;

public class BackFactory : WebApplicationFactory<Back::Program>
{
    // Só guarda spans de traces abertos pelos testes; senão a suíte inteira fica em memória.
    public TelemetryCollection<MetricSnapshot> Metrics { get; } = new();
    public TelemetryCollection<Activity> Spans { get; } = new(BackFactoryTelemetry.IsTestTrace);

    // O CreateClient sobre Kestrel cria um handler (e um pool de conexões) por client e só os descarta junto com a factory.
    // Na suíte inteira isso esgota as portas efêmeras do Windows (WSAENOBUFS), então todos os clients dividem este pool.
    private readonly SocketsHttpHandler _connections = new() { AllowAutoRedirect = false, UseCookies = false };

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
            services.AddSingleton<IStartupFilter, ThrowExceptionStartupFilter>();
            services.AddSingleton<IStartupFilter, RequestsCounterStartupFilter>();

            services.Configure<PasswordHasherOptions>(o => o.IterationCount = 1);

            services.ConfigureOpenTelemetryTracerProvider(tracing => tracing
                .AddSource(BackFactoryTelemetry.TestsActivitySource.Name)
                .AddInMemoryExporter(Spans));

            services.ConfigureOpenTelemetryMeterProvider(metrics => metrics
                .AddInMemoryExporter(Metrics));
        });
    }

    public TestsHttpClient GetTestsClient(bool followRedirects = true)
    {
        HttpMessageHandler handler = new CookieContainerHandler { InnerHandler = _connections };
        if (followRedirects) handler = new RedirectHandler { InnerHandler = handler };

        var client = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = ClientOptions.BaseAddress,
            Timeout = TimeSpan.FromHours(1),
        };

        return new TestsHttpClient(client);
    }

    public EstudDbContext GetDbContext([CallerMemberName] string operation = "")
    {
        var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<EstudDbContext>();
        ctx.Enrich($"Tests.{operation}");
        return ctx;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _connections.Dispose();
    }

    public ISchedulerFactory GetSchedulerFactory()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
    }
}
