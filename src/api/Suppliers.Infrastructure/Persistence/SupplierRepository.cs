using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Common;
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

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("The supplier was changed by someone else. Reload it and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            // Another request inserted the same name and city between our ExistsAsync check and this save.
            throw new ConflictException("A supplier with the same name already exists in this city.", ex);
        }
    }

    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
}
