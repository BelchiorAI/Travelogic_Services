using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Suppliers.Common;

/// <summary>Explicit, hand-written mapping: easy to read and to debug.</summary>
public static class Mappings
{
    public static SupplierDto ToDto(this Supplier supplier) => new(
        supplier.Id,
        supplier.Name,
        supplier.Type,
        supplier.Description,
        supplier.ContactName,
        supplier.Email,
        supplier.Phone,
        supplier.Website,
        supplier.AddressLine,
        supplier.City,
        supplier.Country,
        supplier.CreatedAt,
        supplier.UpdatedAt,
        supplier.Services.Select(ToDto).ToList(),
        supplier.Media.OrderBy(m => m.UploadedAt).Select(ToDto).ToList());

    public static MediaDto ToDto(this SupplierMedia media) => new(
        media.Id,
        media.Kind,
        media.FileName,
        media.ContentType,
        media.SizeBytes,
        MediaUrls.For(media.Id),
        media.UploadedAt);

    public static ServiceDto ToDto(this Service service) => new(
        service.Id,
        service.Name,
        service.Type,
        service.Description,
        service.Price,
        service.Currency,
        service.PricingUnit,
        service.DurationMinutes,
        service.Capacity,
        service.IsActive);
}
