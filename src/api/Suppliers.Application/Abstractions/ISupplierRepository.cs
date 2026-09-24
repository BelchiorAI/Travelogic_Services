using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Abstractions;

/// <summary>Write-side port for the Supplier aggregate. Implemented in Infrastructure.</summary>
public interface ISupplierRepository
{
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken);

    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string name, string city, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
