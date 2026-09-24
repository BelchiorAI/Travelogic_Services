using Suppliers.Domain.Common;

namespace Suppliers.Domain.Suppliers;

/// <summary>Aggregate root: a business that provides services to the tour operator.</summary>
public sealed class Supplier
{
    private readonly List<Service> _services = [];
    private readonly List<SupplierMedia> _media = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public SupplierType Type { get; private set; }
    public string? Description { get; private set; }
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }
    public string? AddressLine { get; private set; }
    public string City { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<Service> Services => _services.AsReadOnly();
    public IReadOnlyCollection<SupplierMedia> Media => _media.AsReadOnly();

    // For EF Core.
    private Supplier() { }

    public static Supplier Create(
        string name,
        SupplierType type,
        string city,
        string country,
        DateTimeOffset now,
        string? description = null,
        string? contactName = null,
        string? email = null,
        string? phone = null,
        string? website = null,
        string? addressLine = null)
    {
        return new Supplier
        {
            Id = Guid.CreateVersion7(),
            Name = Guard.Required(name, "Supplier name", SupplierLimits.NameMaxLength),
            Type = Guard.Defined(type, "Supplier type"),
            City = Guard.Required(city, "City", SupplierLimits.CityMaxLength),
            Country = Guard.Required(country, "Country", SupplierLimits.CountryMaxLength),
            Description = Guard.Optional(description, "Description", SupplierLimits.DescriptionMaxLength),
            ContactName = Guard.Optional(contactName, "Contact name", SupplierLimits.ContactMaxLength),
            Email = Guard.Optional(email, "Email", SupplierLimits.ContactMaxLength),
            Phone = Guard.Optional(phone, "Phone", SupplierLimits.PhoneMaxLength),
            Website = Guard.Optional(website, "Website", SupplierLimits.WebsiteMaxLength),
            AddressLine = Guard.Optional(addressLine, "Address line", SupplierLimits.AddressLineMaxLength),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public Service AddService(
        string name,
        ServiceType type,
        decimal price,
        string currency,
        PricingUnit pricingUnit,
        string? description = null,
        int? durationMinutes = null,
        int? capacity = null)
    {
        if (_services.Count >= SupplierLimits.MaxServicesPerSupplier)
            throw new DomainException($"A supplier can have at most {SupplierLimits.MaxServicesPerSupplier} services.");

        var service = new Service(Id, name, type, price, currency, pricingUnit, description, durationMinutes, capacity);
        _services.Add(service);
        return service;
    }

    /// <param name="mediaId">Chosen by the caller, because the file is stored under a key derived from it before this call.</param>
    public SupplierMedia AddMedia(
        Guid mediaId,
        MediaKind kind,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        DateTimeOffset now)
    {
        if (_media.Count >= SupplierLimits.MaxMediaPerSupplier)
            throw new DomainException($"A supplier can have at most {SupplierLimits.MaxMediaPerSupplier} photos and videos.");

        var media = new SupplierMedia(mediaId, Id, kind, fileName, contentType, sizeBytes, storageKey, now);
        _media.Add(media);
        UpdatedAt = now;
        return media;
    }

    /// <returns>The removed media, or <c>null</c> when this supplier has no media with that id.</returns>
    public SupplierMedia? RemoveMedia(Guid mediaId, DateTimeOffset now)
    {
        var media = _media.FirstOrDefault(m => m.Id == mediaId);
        if (media is null)
            return null;

        _media.Remove(media);
        UpdatedAt = now;
        return media;
    }
}
