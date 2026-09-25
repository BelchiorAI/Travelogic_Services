using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Suppliers.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;

namespace Suppliers.IntegrationTests.Infrastructure;

/// <summary>Runs the real API against a real SQL Server in a container, shared by every test in the collection.</summary>
public sealed class SuppliersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    // The same S3-compatible gateway as docker-compose.yml, so uploads use real signed URLs.
    private readonly IContainer _s3 = new ContainerBuilder("versity/versitygw:latest")
        .WithEnvironment("ROOT_ACCESS_KEY_ID", "test-access")
        .WithEnvironment("ROOT_SECRET_ACCESS_KEY", "test-secret")
        .WithCommand("--port", ":7070", "--health", "/health", "posix", "/data")
        .WithTmpfsMount("/data")
        .WithPortBinding(7070, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(7070).ForPath("/health")))
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sqlServer.StartAsync(), _s3.StartAsync());

        // Creating the client starts the host; migrations (and seeding) then run in the background,
        // so wait until /health/ready says they're done before any test touches the database.
        using var client = CreateClient();
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while ((await client.GetAsync("/health/ready")).StatusCode != System.Net.HttpStatusCode.OK)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("The API didn't become ready within 2 minutes.");
            await Task.Delay(250);
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
        await _s3.DisposeAsync();
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
        builder.UseSetting("Media:S3:ServiceUrl", $"http://{_s3.Hostname}:{_s3.GetMappedPublicPort(7070)}");
        builder.UseSetting("Media:S3:AccessKey", "test-access");
        builder.UseSetting("Media:S3:SecretKey", "test-secret");
        builder.UseSetting("Media:S3:CreateBucketIfMissing", "true");

        // AI extraction is enabled, but talks to a fake model so no test ever calls a real one.
        builder.UseSetting("Ai:Enabled", "true");
        builder.UseSetting("Ai:Model", "fake-model");
        builder.UseSetting("Ai:ApiKey", "fake-key");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IChatClient>();
            services.AddSingleton<IChatClient>(ChatClient);
        });
    }

    public FakeChatClient ChatClient { get; } = new();

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
