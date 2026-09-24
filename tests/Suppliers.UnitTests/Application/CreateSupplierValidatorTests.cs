using Shouldly;
using Suppliers.Application.Suppliers.Create;
using Suppliers.Domain.Suppliers;

namespace Suppliers.UnitTests.Application;

public class CreateSupplierValidatorTests
{
    private readonly CreateSupplierValidator _validator = new();

    internal static CreateServiceRequest ValidService(string name = "Sunrise Game Drive") => new()
    {
        Name = name,
        Type = ServiceType.Activity,
        Price = 950m,
        Currency = "ZAR",
        PricingUnit = PricingUnit.PerPerson,
        DurationMinutes = 180,
        Capacity = 9,
    };

    internal static CreateSupplierRequest ValidRequest() => new()
    {
        Name = "Marula Bush Lodge",
        Type = SupplierType.Accommodation,
        City = "Hazyview",
        Country = "South Africa",
        Email = "reservations@marulabush.example",
        Website = "https://marulabush.example",
        Services = [ValidService()],
    };

    [Fact]
    public void Valid_request_passes()
    {
        _validator.Validate(ValidRequest()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Request_without_services_passes()
    {
        _validator.Validate(ValidRequest() with { Services = [] }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Invalid_service_price_is_reported_with_indexed_key()
    {
        var request = ValidRequest() with
        {
            Services = [ValidService(), ValidService("Bush Walk") with { Price = -10m }],
        };

        var result = _validator.Validate(request);

        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe("Services[1].Price");
    }

    [Theory]
    [InlineData("zar")]
    [InlineData("ZA")]
    [InlineData("RANDS")]
    [InlineData("")]
    public void Invalid_currency_is_reported(string currency)
    {
        var request = ValidRequest() with { Services = [ValidService() with { Currency = currency }] };

        _validator.Validate(request).Errors.ShouldContain(e => e.PropertyName == "Services[0].Currency");
    }

    [Fact]
    public void Missing_name_city_and_country_are_reported()
    {
        var request = ValidRequest() with { Name = "", City = " ", Country = "" };

        var keys = _validator.Validate(request).Errors.Select(e => e.PropertyName).ToList();

        keys.ShouldBe(["Name", "City", "Country"], ignoreOrder: true);
    }

    [Fact]
    public void Name_over_200_characters_is_reported()
    {
        var request = ValidRequest() with { Name = new string('x', 201) };

        _validator.Validate(request).Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Invalid_email_is_reported()
    {
        _validator.Validate(ValidRequest() with { Email = "not-an-email" })
            .Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("marulabush.example")]
    [InlineData("ftp://marulabush.example")]
    public void Invalid_website_is_reported(string website)
    {
        _validator.Validate(ValidRequest() with { Website = website })
            .Errors.ShouldContain(e => e.PropertyName == "Website");
    }

    [Fact]
    public void Undefined_enum_values_are_reported()
    {
        var request = ValidRequest() with
        {
            Type = (SupplierType)99,
            Services = [ValidService() with { Type = (ServiceType)99, PricingUnit = (PricingUnit)99 }],
        };

        var keys = _validator.Validate(request).Errors.Select(e => e.PropertyName).ToList();

        keys.ShouldBe(["Type", "Services[0].Type", "Services[0].PricingUnit"], ignoreOrder: true);
    }

    [Fact]
    public void Missing_types_price_and_pricing_unit_are_reported_instead_of_defaulting()
    {
        var request = ValidRequest() with
        {
            Type = null,
            Services = [ValidService() with { Type = null, Price = null, PricingUnit = null }],
        };

        var keys = _validator.Validate(request).Errors.Select(e => e.PropertyName).ToList();

        keys.ShouldBe(["Type", "Services[0].Type", "Services[0].Price", "Services[0].PricingUnit"], ignoreOrder: true);
    }

    [Fact]
    public void Non_positive_duration_and_capacity_are_reported()
    {
        var request = ValidRequest() with { Services = [ValidService() with { DurationMinutes = 0, Capacity = -1 }] };

        var keys = _validator.Validate(request).Errors.Select(e => e.PropertyName).ToList();

        keys.ShouldBe(["Services[0].DurationMinutes", "Services[0].Capacity"], ignoreOrder: true);
    }

    [Fact]
    public void More_than_50_services_is_reported()
    {
        var services = Enumerable.Range(1, 51).Select(i => ValidService($"Service {i}")).ToList();

        _validator.Validate(ValidRequest() with { Services = services })
            .Errors.ShouldContain(e => e.PropertyName == "Services");
    }
}
