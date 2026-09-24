using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Shouldly;
using Suppliers.Application.Suppliers.Extract;
using Suppliers.Domain.Suppliers;
using Suppliers.IntegrationTests.Infrastructure;

namespace Suppliers.IntegrationTests;

public class ExtractionApiTests(SuppliersApiFactory factory) : ApiTestBase(factory)
{
    private const string ExtractUrl = "/api/v1/suppliers/extract";
    private const string RateSheet = "Marula Bush Lodge, Hazyview, South Africa. Luxury Safari Suite: R6 800 pppn.";

    private const string ValidModelJson =
        """
        {
          "name": "Marula Bush Lodge",
          "type": "Accommodation",
          "city": "Hazyview",
          "country": "South Africa",
          "services": [
            { "name": "Luxury Safari Suite", "type": "Accommodation", "price": 6800, "currency": "ZAR", "pricingUnit": "PerPersonPerNight" }
          ]
        }
        """;

    [Fact]
    public async Task Valid_model_output_returns_a_draft_without_warnings_and_saves_nothing()
    {
        Factory.ChatClient.RespondWith(ValidModelJson);

        var response = await Client.PostAsJsonAsync(ExtractUrl, new ExtractSupplierDraftRequest(RateSheet), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<ExtractSupplierDraftResult>(Json)).ShouldNotBeNull();
        result.Warnings.ShouldBeEmpty();
        result.Draft.Name.ShouldBe("Marula Bush Lodge");
        result.Draft.Type.ShouldBe(SupplierType.Accommodation);
        result.Draft.Services.ShouldHaveSingleItem().PricingUnit.ShouldBe(PricingUnit.PerPersonPerNight);

        var list = await Client.GetStringAsync(SuppliersUrl);
        list.ShouldContain("\"totalCount\":0");
    }

    [Fact]
    public async Task Invalid_model_output_becomes_warnings_keyed_like_validation_errors()
    {
        Factory.ChatClient.RespondWith(
            """
            { "name": "Marula Bush Lodge", "type": null, "city": "Hazyview", "country": "South Africa",
              "services": [ { "name": "Suite", "type": "Accommodation", "price": -5, "currency": "ZAR", "pricingUnit": null } ] }
            """);

        var response = await Client.PostAsJsonAsync(ExtractUrl, new ExtractSupplierDraftRequest(RateSheet), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var fields = json.RootElement.GetProperty("warnings").EnumerateArray()
            .Select(w => w.GetProperty("field").GetString())
            .ToList();
        fields.ShouldBe(["Type", "Services[0].Price", "Services[0].PricingUnit"], ignoreOrder: true);
        json.RootElement.GetProperty("draft").GetProperty("type").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Unreadable_model_output_returns_200_with_a_general_warning()
    {
        Factory.ChatClient.RespondWith("Sorry, I cannot help with that.");

        var response = await Client.PostAsJsonAsync(ExtractUrl, new ExtractSupplierDraftRequest(RateSheet), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExtractSupplierDraftResult>(Json);
        result!.Warnings.ShouldHaveSingleItem().Field.ShouldBe("");
    }

    [Fact]
    public async Task Model_failure_returns_502_problem()
    {
        Factory.ChatClient.Throw(new HttpRequestException("connection refused"));

        var response = await Client.PostAsJsonAsync(ExtractUrl, new ExtractSupplierDraftRequest(RateSheet), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("connection refused");
    }

    [Fact]
    public async Task Text_over_the_limit_returns_400_without_calling_the_model()
    {
        var text = new string('x', ExtractSupplierDraftRequest.MaxTextLength + 1);

        var response = await Client.PostAsJsonAsync(ExtractUrl, new ExtractSupplierDraftRequest(text), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        Factory.ChatClient.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Features_reports_ai_extraction_enabled()
    {
        (await Client.GetStringAsync("/api/v1/features")).ShouldBe("""{"aiExtraction":true}""");
    }

    [Fact]
    public async Task Disabled_feature_returns_503_and_features_reports_false()
    {
        await using var disabledApi = Factory.WithWebHostBuilder(b => b.UseSetting("Ai:Enabled", "false"));
        using var client = disabledApi.CreateClient();

        var response = await client.PostAsJsonAsync(ExtractUrl, new ExtractSupplierDraftRequest(RateSheet), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await client.GetStringAsync("/api/v1/features")).ShouldBe("""{"aiExtraction":false}""");
        Factory.ChatClient.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task More_than_10_requests_a_minute_returns_429()
    {
        // A separate host gets its own rate limiter, so this test cannot starve the others.
        await using var api = Factory.WithWebHostBuilder(_ => { });
        using var client = api.CreateClient();
        Factory.ChatClient.RespondWith(ValidModelJson);

        for (var i = 0; i < 10; i++)
            (await client.PostAsJsonAsync(ExtractUrl, new ExtractSupplierDraftRequest(RateSheet), Json)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var rejected = await client.PostAsJsonAsync(ExtractUrl, new ExtractSupplierDraftRequest(RateSheet), Json);

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.Contains("Retry-After").ShouldBeTrue();
    }
}
