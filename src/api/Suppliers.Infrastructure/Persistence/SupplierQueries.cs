using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.List;

namespace Suppliers.Infrastructure.Persistence;

internal sealed class SupplierQueries(SuppliersDbContext db) : ISupplierQueries
{
    public Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.Type,
                s.Description,
                s.ContactName,
                s.Email,
                s.Phone,
                s.Website,
                s.Address,
                s.City,
                s.Country,
                s.CreatedAt,
                s.UpdatedAt,
                s.Services
                    .OrderBy(sv => sv.Name)
                    .Select(sv => new ServiceDto(
                        sv.Id,
                        sv.Name,
                        sv.Type,
                        sv.Description,
                        sv.Price,
                        sv.Currency,
                        sv.PricingUnit,
                        sv.DurationMinutes,
                        sv.Capacity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<SupplierSummaryDto>> ListAsync(ListSuppliersQuery query, CancellationToken cancellationToken)
    {
        var suppliers = db.Suppliers.AsNoTracking();

        if (query.Search is { } search)
            suppliers = suppliers.Where(s => s.Name.Contains(search));

        if (query.Type is { } type)
            suppliers = suppliers.Where(s => s.Type == type);

        var totalCount = await suppliers.CountAsync(cancellationToken);

        var items = await suppliers
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SupplierSummaryDto(
                s.Id,
                s.Name,
                s.Type,
                s.City,
                s.Country,
                s.Email,
                s.Phone,
                s.Services.Count,
                s.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }
}
