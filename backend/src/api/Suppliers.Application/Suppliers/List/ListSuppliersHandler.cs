using Suppliers.Application.Abstractions;
using Suppliers.Application.Suppliers.Common;

namespace Suppliers.Application.Suppliers.List;

public sealed class ListSuppliersHandler(ISupplierQueries queries)
{
    public Task<PagedResult<SupplierSummaryDto>> HandleAsync(ListSuppliersQuery query, CancellationToken cancellationToken) =>
        queries.ListAsync(query.Normalise(), cancellationToken);
}
