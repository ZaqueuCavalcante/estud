using Npgsql;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;

namespace Estud.Back.Database;

[ExcludeFromCodeCoverage]
public class EstudDbContextFactory : IDesignTimeDbContextFactory<EstudDbContext>
{
    public EstudDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Host=localhost;Database=estud;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<EstudDbContext>()
            .UseSnakeCaseNamingConvention()
            .UseNpgsql(connectionString)
            .Options;

        var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

        return new EstudDbContext(options, dataSource, null!);
    }
}
