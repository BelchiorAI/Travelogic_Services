using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.List;

namespace Suppliers.Application.Abstractions;

/// <summary>Read-side port: projects straight to DTOs without tracking. Implemented in Infrastructure.</summary>
public interface ISupplierQueries
{
    Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SupplierSummaryDto>> ListAsync(ListSuppliersQuery query, CancellationToken cancellationToken);
}
