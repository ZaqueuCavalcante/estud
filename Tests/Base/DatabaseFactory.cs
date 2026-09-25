using Npgsql;
using Estud.Back.Settings;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Configuration;

namespace Estud.Tests.Base;

public class DatabaseFactory
{
    private readonly PostgreSqlContainer _container;
    private readonly NpgsqlConnectionStringBuilder _settings;

    public DatabaseFactory()
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Testing.json");
        var connectionString = new ConfigurationBuilder().AddJsonFile(configPath).Build().Database.ConnectionString;
        _settings = new NpgsqlConnectionStringBuilder(connectionString);

        _container = new PostgreSqlBuilder("postgres:18")
            .WithName(_settings.Database!)
            .WithDatabase(_settings.Database!)
            .WithUsername(_settings.Username!)
            .WithPassword(_settings.Password!)
            .WithPortBinding(_settings.Port, 5432)
            .WithTmpfsMount("/var/lib/postgresql")
            .WithReuse(true)
            .Build();
    }

    public string ConnectionString => _settings.ConnectionString;

    public async Task StartAsync()
    {
        await _container.StartAsync();
    }
}
