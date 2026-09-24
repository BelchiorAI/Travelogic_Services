using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Abstractions;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Infrastructure.Persistence;

internal sealed class SupplierRepository(SuppliersDbContext db) : ISupplierRepository
{
    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken) =>
        await db.Suppliers.AddAsync(supplier, cancellationToken);

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Suppliers
            .Include(s => s.Services)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(string name, string city, CancellationToken cancellationToken)
    {
        // Trim to match how the domain stores values; SQL Server's default collation makes this case-insensitive.
        var trimmedName = name.Trim();
        var trimmedCity = city.Trim();
        return db.Suppliers.AnyAsync(s => s.Name == trimmedName && s.City == trimmedCity, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
