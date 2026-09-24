using Suppliers.Domain.Common;

namespace Suppliers.Domain.Suppliers;

/// <summary>A bookable product offered by a supplier. Only <see cref="Supplier.AddService"/> can create one.</summary>
public sealed class Service
{
    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public string Name { get; private set; } = null!;
    public ServiceType Type { get; private set; }
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = null!;
    public PricingUnit PricingUnit { get; private set; }
    public int? DurationMinutes { get; private set; }
    public int? Capacity { get; private set; }

    /// <summary>New services are bookable; deactivating keeps history instead of deleting.</summary>
    public bool IsActive { get; private set; }

    // For EF Core.
    private Service() { }

    internal Service(
        Guid supplierId,
        string name,
        ServiceType type,
        decimal price,
        string currency,
        PricingUnit pricingUnit,
        string? description,
        int? durationMinutes,
        int? capacity)
    {
        if (price < 0)
            throw new DomainException("Price cannot be negative.");

        Id = Guid.CreateVersion7();
        SupplierId = supplierId;
        Name = Guard.Required(name, "Service name", SupplierLimits.NameMaxLength);
        Type = Guard.Defined(type, "Service type");
        Description = Guard.Optional(description, "Service description", SupplierLimits.DescriptionMaxLength);
        Price = price;
        Currency = ParseCurrency(currency);
        PricingUnit = Guard.Defined(pricingUnit, "Pricing unit");
        DurationMinutes = Guard.PositiveOrNull(durationMinutes, "Duration");
        Capacity = Guard.PositiveOrNull(capacity, "Capacity");
        IsActive = true;
    }

    private static string ParseCurrency(string? currency)
    {
        if (currency is not { Length: SupplierLimits.CurrencyLength } || !currency.All(char.IsAsciiLetterUpper))
            throw new DomainException("Currency must be a 3-letter uppercase ISO code, e.g. ZAR.");

        return currency;
    }
}
