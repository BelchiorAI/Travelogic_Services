using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shouldly;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Domain.Suppliers;
using Suppliers.IntegrationTests.Infrastructure;

namespace Suppliers.IntegrationTests;

public class SupplierApiTests(SuppliersApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Post_valid_supplier_returns_201_and_location_returns_it_with_services()
    {
        var request = Supplier() with { Services = [Service("Game Drive"), Service("Bush Walk", 650m)] };

        var response = await Client.PostAsJsonAsync(SuppliersUrl, request, Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var location = response.Headers.Location.ShouldNotBeNull();
        var created = await response.Content.ReadFromJsonAsync<SupplierDto>(Json);
        location.ToString().ShouldEndWith($"{SuppliersUrl}/{created!.Id}");

        var fetched = await Client.GetFromJsonAsync<SupplierDto>(location, Json);

        fetched.ShouldNotBeNull();
        fetched.Name.ShouldBe("Marula Bush Lodge");
        fetched.Type.ShouldBe(SupplierType.Accommodation);
        fetched.Services.Select(s => s.Name).ShouldBe(["Bush Walk", "Game Drive"]);
        fetched.Services.Single(s => s.Name == "Bush Walk").Price.ShouldBe(650m);
    }

    [Fact]
    public async Task Json_shape_matches_the_frontend_contract()
    {
        var created = await CreateAsync(Supplier() with { AddressLine = "R536, Sabie Road" });

        using var json = JsonDocument.Parse(await Client.GetStringAsync($"{SuppliersUrl}/{created.Id}"));

        json.RootElement.GetProperty("type").GetString().ShouldBe("Accommodation");
        json.RootElement.GetProperty("addressLine").GetString().ShouldBe("R536, Sabie Road");
        var service = json.RootElement.GetProperty("services")[0];
        service.GetProperty("pricingUnit").GetString().ShouldBe("PerPerson");
        service.GetProperty("durationMinutes").GetInt32().ShouldBe(180);
        service.GetProperty("isActive").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Post_with_invalid_service_returns_400_with_indexed_error_key()
    {
        var request = Supplier() with { Services = [Service(price: -1m)] };

        var response = await Client.PostAsJsonAsync(SuppliersUrl, request, Json);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = problem.RootElement.GetProperty("errors");
        errors.TryGetProperty("Services[0].Price", out _).ShouldBeTrue(errors.ToString());
    }

    [Fact]
    public async Task Post_with_unknown_enum_value_returns_400_problem()
    {
        const string body = """{ "name": "X", "type": "Spaceship", "city": "Cape Town", "country": "South Africa", "services": [] }""";

        var response = await Client.PostAsync(SuppliersUrl, new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Post_with_price_as_a_string_returns_400_problem()
    {
        const string body = """
            { "name": "X", "type": "Activity", "city": "Knysna", "country": "South Africa",
              "services": [ { "name": "Kayak", "type": "Activity", "price": "450", "currency": "ZAR", "pricingUnit": "PerPerson" } ] }
            """;

        var response = await Client.PostAsync(SuppliersUrl, new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_duplicate_name_and_city_returns_409()
    {
        await CreateAsync(Supplier());

        var response = await Client.PostAsJsonAsync(SuppliersUrl, Supplier(name: "marula bush lodge"), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Same_name_in_a_different_city_is_allowed()
    {
        await CreateAsync(Supplier(city: "Hazyview"));

        await CreateAsync(Supplier(city: "Hoedspruit"));
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        var response = await Client.GetAsync($"{SuppliersUrl}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_supports_search_type_filter_and_paging()
    {
        await CreateAsync(Supplier("Marula Bush Lodge", type: SupplierType.Accommodation));
        await CreateAsync(Supplier("Kruger Gate Lodge", type: SupplierType.Accommodation));
        await CreateAsync(Supplier("Lodge Transfers", type: SupplierType.Transport));
        await CreateAsync(Supplier("Harbourview Hotel", type: SupplierType.Accommodation));

        var all = await Client.GetFromJsonAsync<PagedResult<SupplierSummaryDto>>(SuppliersUrl, Json);
        all!.TotalCount.ShouldBe(4);
        all.Items.Select(s => s.Name).ShouldBe(["Harbourview Hotel", "Kruger Gate Lodge", "Lodge Transfers", "Marula Bush Lodge"]);
        all.Items[0].ServiceCount.ShouldBe(1);

        var search = await Client.GetFromJsonAsync<PagedResult<SupplierSummaryDto>>($"{SuppliersUrl}?search=lodge", Json);
        search!.TotalCount.ShouldBe(3);

        var filtered = await Client.GetFromJsonAsync<PagedResult<SupplierSummaryDto>>($"{SuppliersUrl}?search=lodge&type=Accommodation", Json);
        filtered!.Items.Select(s => s.Name).ShouldBe(["Kruger Gate Lodge", "Marula Bush Lodge"]);

        var page2 = await Client.GetFromJsonAsync<PagedResult<SupplierSummaryDto>>($"{SuppliersUrl}?page=2&pageSize=3", Json);
        page2!.TotalCount.ShouldBe(4);
        page2.TotalPages.ShouldBe(2);
        page2.Items.ShouldHaveSingleItem().Name.ShouldBe("Marula Bush Lodge");
    }

    [Theory]
    [InlineData("cape town")]
    [InlineData("tablebay.example")]
    [InlineData("table bay")]
    public async Task Search_matches_name_city_or_email(string term)
    {
        await CreateAsync(Supplier("Table Bay Hotel", city: "Cape Town") with { Email = "stay@tablebay.example" });
        await CreateAsync(Supplier("Marula Bush Lodge", city: "Hazyview"));

        var result = await Client.GetFromJsonAsync<PagedResult<SupplierSummaryDto>>(
            $"{SuppliersUrl}?search={Uri.EscapeDataString(term)}", Json);

        result!.Items.ShouldHaveSingleItem().Name.ShouldBe("Table Bay Hotel");
    }

    [Fact]
    public async Task List_caps_page_size_at_50()
    {
        var result = await Client.GetFromJsonAsync<PagedResult<SupplierSummaryDto>>($"{SuppliersUrl}?pageSize=1000", Json);

        result!.PageSize.ShouldBe(50);
    }
}
