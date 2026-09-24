using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Suppliers.Create;

// Enums and price are nullable so a missing value fails validation instead of silently
// defaulting (e.g. to Accommodation or 0), and so an AI draft can leave unknown fields empty.

public sealed record CreateSupplierRequest
{
    public string Name { get; init; } = string.Empty;
    public SupplierType? Type { get; init; }
    public string? Description { get; init; }
    public string? ContactName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public string? Address { get; init; }
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public IReadOnlyList<CreateServiceRequest> Services { get; init; } = [];
}

public sealed record CreateServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public ServiceType? Type { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public PricingUnit? PricingUnit { get; init; }
    public int? DurationMinutes { get; init; }
    public int? Capacity { get; init; }
}
