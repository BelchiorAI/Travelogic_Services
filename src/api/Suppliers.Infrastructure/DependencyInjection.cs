using System.ClientModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using Suppliers.Application.Abstractions;
using Suppliers.Infrastructure.Ai;
using Suppliers.Infrastructure.Persistence;

namespace Suppliers.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "SuppliersDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<SuppliersDbContext>(options => options
            .UseSqlServer(connectionString, sql => sql
                .MigrationsHistoryTable("__EFMigrationsHistory", SuppliersDbContext.Schema)
                .EnableRetryOnFailure())
            .UseSeeding((context, _) => SeedData.Seed(context))
            .UseAsyncSeeding((context, _, cancellationToken) => SeedData.SeedAsync(context, cancellationToken)));

        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISupplierQueries, SupplierQueries>();

        services.AddSupplierExtraction(configuration);

        return services;
    }

    private static void AddSupplierExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(AiOptions.SectionName);
        services.Configure<AiOptions>(section);
        var ai = section.Get<AiOptions>() ?? new AiOptions();

        if (!ai.IsConfigured)
        {
            services.AddSingleton<ISupplierExtractionService, DisabledSupplierExtractionService>();
            return;
        }

        // Any OpenAI-compatible provider works; Endpoint points elsewhere (e.g. Gemini) when set.
        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(ai.Endpoint))
            clientOptions.Endpoint = new Uri(ai.Endpoint);

        services.AddChatClient(_ => new ChatClient(ai.Model, new ApiKeyCredential(ai.ApiKey), clientOptions).AsIChatClient());
        services.AddScoped<ISupplierExtractionService, AiSupplierExtractionService>();
    }

    /// <summary>Applies pending migrations; EF Core then runs the seeding callbacks.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SuppliersDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
