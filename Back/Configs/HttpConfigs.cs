using Estud.Back.Converters;
using Microsoft.AspNetCore.HttpOverrides;

namespace Estud.Back.Configs;

public static class HttpConfigs
{
    public static void AddHttpConfigs(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new EstudStringEnumConverter()));

        builder.Services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(new ProducesAttribute("application/json"));
            options.Filters.Add(new ConsumesAttribute("application/json"));

            options.Conventions.Add(new HideAdminFromDocsConvention());
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddRouting(options => options.LowercaseUrls = true);

        builder.Services.AddHttpClient();
    }

    public static void UseCustomForwardedHeaders(this WebApplication app)
    {
        var fwd = new ForwardedHeadersOptions
        {
            ForwardLimit = null,
            RequireHeaderSymmetry = false,
            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedFor,
        };

        fwd.KnownProxies.Clear();
        fwd.KnownIPNetworks.Clear();

        app.UseForwardedHeaders(fwd);
    }

    public static void UseHttpsConfigs(this WebApplication app)
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    public static void UseApiPrefix(this IApplicationBuilder app)
    {
        app.UsePathBase("/api");
    }

    public static void UseExceptions(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionsMiddleware>();
    }

    public static void UseControllers(this IApplicationBuilder app)
    {
        app.UseEndpoints(options =>
        {
            options.MapControllers();

            options.MapOpenApi();
            options.MapScalarDocs();
        });
    }

    public static void UseLogs(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging();
    }
}
