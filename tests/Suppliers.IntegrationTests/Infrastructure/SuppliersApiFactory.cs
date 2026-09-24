using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Suppliers.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Suppliers.IntegrationTests.Infrastructure;

/// <summary>Runs the real API against a real SQL Server in a container, shared by every test in the collection.</summary>
public sealed class SuppliersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();

        // Creating the client builds the host, which applies migrations (and seeds) on startup.
        CreateClient().Dispose();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "SuppliersTests",
        }.ConnectionString;

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SuppliersDb", connectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
    }

    /// <summary>Gives a test a clean database. Services are removed by the cascade delete.</summary>
    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SuppliersDbContext>();
        await db.Suppliers.ExecuteDeleteAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SuppliersApiFactory>
{
    public const string Name = "Api";
}
