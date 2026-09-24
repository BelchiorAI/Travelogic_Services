using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Suppliers.Common;

public sealed record SupplierDto(
    Guid Id,
    string Name,
    SupplierType Type,
    string? Description,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Website,
    string? AddressLine,
    string City,
    string Country,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ServiceDto> Services,
    IReadOnlyList<MediaDto> Media);

public sealed record ServiceDto(
    Guid Id,
    string Name,
    ServiceType Type,
    string? Description,
    decimal Price,
    string Currency,
    PricingUnit PricingUnit,
    int? DurationMinutes,
    int? Capacity,
    bool IsActive);

public sealed record SupplierSummaryDto(
    Guid Id,
    string Name,
    SupplierType Type,
    string City,
    string Country,
    string? Email,
    string? Phone,
    int ServiceCount,
    DateTimeOffset CreatedAt,
    string? CoverImageUrl);

/// <param name="Url">Relative to the API's base address, e.g. "/api/v1/media/{id}".</param>
public sealed record MediaDto(
    Guid Id,
    MediaKind Kind,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Url,
    DateTimeOffset UploadedAt);

/// <summary>Where a media file is stored, for serving it. Never sent to clients.</summary>
public sealed record MediaFileInfo(string StorageKey, string ContentType, DateTimeOffset UploadedAt);

public static class MediaUrls
{
    public static string For(Guid mediaId) => $"/api/v1/media/{mediaId}";
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
