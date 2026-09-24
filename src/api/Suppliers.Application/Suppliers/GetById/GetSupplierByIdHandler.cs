using Suppliers.Application.Abstractions;
using Suppliers.Application.Suppliers.Common;

namespace Suppliers.Application.Suppliers.GetById;

public sealed class GetSupplierByIdHandler(ISupplierQueries queries)
{
    /// <returns>The supplier, or <c>null</c> when it does not exist.</returns>
    public Task<SupplierDto?> HandleAsync(Guid id, CancellationToken cancellationToken) =>
        queries.GetByIdAsync(id, cancellationToken);
}
