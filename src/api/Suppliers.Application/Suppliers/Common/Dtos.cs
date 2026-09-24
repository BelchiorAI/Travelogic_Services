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
    string? Address,
    string City,
    string Country,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ServiceDto> Services);

public sealed record ServiceDto(
    Guid Id,
    string Name,
    ServiceType Type,
    string? Description,
    decimal Price,
    string Currency,
    PricingUnit PricingUnit,
    int? DurationMinutes,
    int? Capacity);

public sealed record SupplierSummaryDto(
    Guid Id,
    string Name,
    SupplierType Type,
    string City,
    string Country,
    string? Email,
    string? Phone,
    int ServiceCount,
    DateTimeOffset CreatedAt);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
