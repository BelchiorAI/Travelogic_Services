using System.Net;
using System.Text.Json;
using Shouldly;
using Suppliers.IntegrationTests.Infrastructure;

namespace Suppliers.IntegrationTests;

public class PlatformTests(SuppliersApiFactory factory) : ApiTestBase(factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_return_200(string url)
    {
        var response = await Client.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApi_document_describes_the_supplier_endpoints()
    {
        using var json = JsonDocument.Parse(await Client.GetStringAsync("/openapi/v1.json"));

        var paths = json.RootElement.GetProperty("paths");
        paths.TryGetProperty("/api/v1/suppliers", out _).ShouldBeTrue(paths.ToString());
        paths.TryGetProperty("/api/v1/suppliers/{id}", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task OpenApi_document_types_price_as_a_plain_number_for_generated_clients()
    {
        using var json = JsonDocument.Parse(await Client.GetStringAsync("/openapi/v1.json"));

        var price = json.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("ServiceDto").GetProperty("properties").GetProperty("price");

        price.GetProperty("type").GetString().ShouldBe("number");
    }
}
