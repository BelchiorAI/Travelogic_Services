using Shouldly;
using Suppliers.Domain.Common;
using Suppliers.Domain.Suppliers;

namespace Suppliers.UnitTests.Domain;

public class SupplierTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private static Supplier ValidSupplier() =>
        Supplier.Create("Kruger Bush Lodge", SupplierType.Accommodation, "Hazyview", "South Africa", Now);

    private static Service AddValidService(Supplier supplier, string name = "Luxury Suite") =>
        supplier.AddService(name, ServiceType.Accommodation, 4500m, "ZAR", PricingUnit.PerRoomPerNight);

    [Fact]
    public void Create_with_valid_data_sets_properties()
    {
        var supplier = Supplier.Create(
            "  Kruger Bush Lodge  ", SupplierType.Accommodation, "Hazyview", "South Africa", Now,
            email: "bookings@krugerbush.example", phone: "  ");

        supplier.Id.ShouldNotBe(Guid.Empty);
        supplier.Name.ShouldBe("Kruger Bush Lodge");
        supplier.Email.ShouldBe("bookings@krugerbush.example");
        supplier.Phone.ShouldBeNull();
        supplier.CreatedAt.ShouldBe(Now);
        supplier.UpdatedAt.ShouldBe(Now);
        supplier.Services.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_without_name_is_rejected(string? name)
    {
        Should.Throw<DomainException>(() =>
            Supplier.Create(name!, SupplierType.Activity, "Cape Town", "South Africa", Now));
    }

    [Fact]
    public void Create_with_name_over_200_characters_is_rejected()
    {
        Should.Throw<DomainException>(() =>
            Supplier.Create(new string('a', 201), SupplierType.Activity, "Cape Town", "South Africa", Now));
    }

    [Fact]
    public void Create_with_name_of_exactly_200_characters_is_allowed()
    {
        Supplier.Create(new string('a', 200), SupplierType.Activity, "Cape Town", "South Africa", Now)
            .Name.Length.ShouldBe(200);
    }

    [Fact]
    public void Create_without_city_is_rejected()
    {
        Should.Throw<DomainException>(() =>
            Supplier.Create("Shark Cage Co", SupplierType.Activity, "", "South Africa", Now));
    }

    [Fact]
    public void Create_with_undefined_supplier_type_is_rejected()
    {
        Should.Throw<DomainException>(() =>
            Supplier.Create("Shark Cage Co", (SupplierType)99, "Gansbaai", "South Africa", Now));
    }

    [Fact]
    public void AddService_with_valid_data_adds_it_to_the_supplier()
    {
        var supplier = ValidSupplier();

        var service = supplier.AddService(
            "Morning Game Drive", ServiceType.Activity, 850m, "ZAR", PricingUnit.PerPerson,
            durationMinutes: 180, capacity: 10);

        supplier.Services.ShouldHaveSingleItem().ShouldBeSameAs(service);
        service.SupplierId.ShouldBe(supplier.Id);
        service.Price.ShouldBe(850m);
        service.DurationMinutes.ShouldBe(180);
    }

    [Fact]
    public void AddService_with_zero_price_is_allowed()
    {
        var supplier = ValidSupplier();

        supplier.AddService("Welcome drink", ServiceType.Meal, 0m, "ZAR", PricingUnit.PerPerson)
            .Price.ShouldBe(0m);
    }

    [Fact]
    public void AddService_with_negative_price_is_rejected()
    {
        var supplier = ValidSupplier();

        Should.Throw<DomainException>(() =>
            supplier.AddService("Suite", ServiceType.Accommodation, -1m, "ZAR", PricingUnit.PerRoomPerNight));
        supplier.Services.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("zar")]
    [InlineData("ZA")]
    [InlineData("ZARR")]
    [InlineData("Z1R")]
    [InlineData("")]
    [InlineData(null)]
    public void AddService_with_invalid_currency_is_rejected(string? currency)
    {
        var supplier = ValidSupplier();

        Should.Throw<DomainException>(() =>
            supplier.AddService("Suite", ServiceType.Accommodation, 100m, currency!, PricingUnit.PerRoomPerNight));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddService_with_non_positive_duration_is_rejected(int duration)
    {
        var supplier = ValidSupplier();

        Should.Throw<DomainException>(() =>
            supplier.AddService("Drive", ServiceType.Activity, 100m, "ZAR", PricingUnit.PerPerson, durationMinutes: duration));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddService_with_non_positive_capacity_is_rejected(int capacity)
    {
        var supplier = ValidSupplier();

        Should.Throw<DomainException>(() =>
            supplier.AddService("Drive", ServiceType.Activity, 100m, "ZAR", PricingUnit.PerPerson, capacity: capacity));
    }

    [Fact]
    public void AddService_with_undefined_pricing_unit_is_rejected()
    {
        var supplier = ValidSupplier();

        Should.Throw<DomainException>(() =>
            supplier.AddService("Drive", ServiceType.Activity, 100m, "ZAR", (PricingUnit)42));
    }

    [Fact]
    public void AddService_allows_up_to_50_services_and_rejects_the_51st()
    {
        var supplier = ValidSupplier();
        for (var i = 1; i <= SupplierLimits.MaxServicesPerSupplier; i++)
            AddValidService(supplier, $"Room {i}");

        supplier.Services.Count.ShouldBe(50);
        Should.Throw<DomainException>(() => AddValidService(supplier, "Room 51"));
        supplier.Services.Count.ShouldBe(50);
    }
}
