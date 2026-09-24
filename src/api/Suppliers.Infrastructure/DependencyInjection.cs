using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Suppliers.Application.Abstractions;
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

        return services;
    }

    /// <summary>Applies pending migrations; EF Core then runs the seeding callbacks.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SuppliersDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
