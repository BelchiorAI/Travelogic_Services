using Microsoft.EntityFrameworkCore;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Infrastructure.Persistence;

public sealed class SuppliersDbContext(DbContextOptions<SuppliersDbContext> options) : DbContext(options)
{
    public const string Schema = "supplier";

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SuppliersDbContext).Assembly);
    }
}
