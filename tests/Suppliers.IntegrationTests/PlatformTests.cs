using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
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

    [Theory]
    [InlineData("true", "http://localhost:8081", true)]
    [InlineData("true", "https://evil.example", false)]
    [InlineData("false", "http://localhost:8081", false)]
    public async Task Cors_allows_any_localhost_port_only_when_enabled(string allowLocalhost, string origin, bool allowed)
    {
        await using var api = Factory.WithWebHostBuilder(b => b.UseSetting("Cors:AllowLocalhost", allowLocalhost));
        using var client = api.CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/suppliers");
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(preflight);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBe(allowed);
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
