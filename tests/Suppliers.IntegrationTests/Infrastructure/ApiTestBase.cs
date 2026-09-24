using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.Create;
using Suppliers.Domain.Suppliers;

namespace Suppliers.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(SuppliersApiFactory factory) : IAsyncLifetime
{
    protected const string SuppliersUrl = "/api/v1/suppliers";

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected HttpClient Client { get; } = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected static CreateServiceRequest Service(string name = "Sunrise Game Drive", decimal price = 950m) => new()
    {
        Name = name,
        Type = ServiceType.Activity,
        Price = price,
        Currency = "ZAR",
        PricingUnit = PricingUnit.PerPerson,
        DurationMinutes = 180,
        Capacity = 9,
    };

    protected static CreateSupplierRequest Supplier(
        string name = "Marula Bush Lodge",
        string city = "Hazyview",
        SupplierType type = SupplierType.Accommodation) => new()
    {
        Name = name,
        Type = type,
        City = city,
        Country = "South Africa",
        Email = "reservations@marulabush.example",
        Services = [Service()],
    };

    protected async Task<SupplierDto> CreateAsync(CreateSupplierRequest request)
    {
        var response = await Client.PostAsJsonAsync(SuppliersUrl, request, Json);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SupplierDto>(Json)).ShouldNotBeNull();
    }
}
