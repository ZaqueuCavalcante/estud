using Amazon.S3;
using Estud.Back.Emails;
using Estud.Back.Google;
using Estud.Back.Storage;
using Estud.Back.Auth.Managers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Estud.Back.Configs;

public static class ServicesConfigs
{
    public static void AddServicesConfigs(this WebApplicationBuilder builder)
    {
        builder.Services.AddServices(typeof(IEstudService));

        builder.Services.AddScoped<DnsManager>();

        builder.Services.AddScoped<IEmailsService, EmailsService>();
        builder.Services.AddScoped<IGoogleService, GoogleService>();
        builder.Services.AddScoped<IStorageService, R2StorageService>();

        builder.Services.AddSingleton<IAmazonS3>(sp =>
        {
            var settings = sp.GetRequiredService<StorageSettings>();
            var config = new AmazonS3Config
            {
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
                ServiceURL = settings.ServiceUrl,
            };
            return new AmazonS3Client(settings.AccessKeyId, settings.SecretAccessKey, config);
        });

        if (EnvironmentExtensions.IsTesting())
        {
            builder.Services.Replace(ServiceDescriptor.Singleton<IGoogleService, FakeGoogleService>());
        }
    }

    private static void AddServices(this IServiceCollection services, Type marker)
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .Where(s => s.FullName.StartsWith(nameof(Back)))
            .SelectMany(s => s.GetTypes())
            .Where(p => marker.IsAssignableFrom(p) && !p.IsInterface)
            .ToList();

        foreach (var type in types)
        {
            services.AddScoped(type);
        }
    }
}
