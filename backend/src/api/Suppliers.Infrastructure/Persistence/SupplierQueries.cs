using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.List;
using Suppliers.Domain.Suppliers;

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
                s.AddressLine,
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
                        sv.Capacity,
                        sv.IsActive))
                    .ToList(),
                s.Media
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => new MediaDto(
                        m.Id,
                        m.Kind,
                        m.FileName,
                        m.ContentType,
                        m.SizeBytes,
                        MediaUrls.For(m.Id),
                        m.UploadedAt))
                    .ToList()))
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<SupplierSummaryDto>> ListAsync(ListSuppliersQuery query, CancellationToken cancellationToken)
    {
        var suppliers = db.Suppliers.AsNoTracking();

        if (query.Search is { } search)
        {
            suppliers = suppliers.Where(s =>
                s.Name.Contains(search) || s.City.Contains(search) || (s.Email != null && s.Email.Contains(search)));
        }

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
                s.CreatedAt,
                // The oldest photo is the cover image; SQL picks its id, C# builds the URL.
                CoverUrl(s.Media
                    .Where(m => m.Kind == MediaKind.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => (Guid?)m.Id)
                    .FirstOrDefault())))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }

    private static string? CoverUrl(Guid? mediaId) => mediaId is { } id ? MediaUrls.For(id) : null;

    public Task<MediaFileInfo?> GetMediaFileInfoAsync(Guid mediaId, CancellationToken cancellationToken) =>
        db.Set<SupplierMedia>()
            .AsNoTracking()
            .Where(m => m.Id == mediaId)
            .Select(m => new MediaFileInfo(m.StorageKey, m.ContentType, m.UploadedAt))
            .FirstOrDefaultAsync(cancellationToken);
}
