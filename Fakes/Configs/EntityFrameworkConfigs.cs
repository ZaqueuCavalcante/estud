using Estud.Back.Database;

namespace Estud.Fakes.Configs;

public static class EntityFrameworkConfigs
{
    public static void AddEntityFrameworkConfigs(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<EstudDbContext>();
    }
}
