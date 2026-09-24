using Npgsql;
using Estud.Tests.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Estud.Tests.Base;

[Parallelizable(ParallelScope.Children)]
public abstract class IntegrationTestBase
{
    protected BackFactory _back = null!;
    protected FakesFactory _fakes = null!;
    protected StorageFactory _storage = null!;
    public const string FrontUrl = "http://localhost:3000";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        EnvironmentExtensions.SetAsTesting();

        _storage = new StorageFactory();
        await Task.WhenAll(ResetEstudDb(), _storage.Start());

        _fakes = new FakesFactory();
        _fakes.StartServer();

        _back = new BackFactory();
        using var scope = _back.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<EstudDbContext>();

        await new DataSeeder(ctx).Run();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _back.DisposeAsync();
        await _fakes.DisposeAsync();
        await _storage.DisposeAsync();
    }

    private static async Task ResetEstudDb()
    {
        var database = new DatabaseFactory();
        await database.StartAsync();

        var dataSource = new NpgsqlDataSourceBuilder(database.ConnectionString).Build();
        var options = new DbContextOptionsBuilder<EstudDbContext>().Options;

        using var ctx = new EstudDbContext(options, dataSource, null);

        if (!database.ConnectionString.Contains("Host=localhost;")) throw new Exception("WRONG TESTS DB");

        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();
    }
}
