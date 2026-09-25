using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Shouldly;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.Media;
using Suppliers.Domain.Suppliers;
using Suppliers.IntegrationTests.Infrastructure;

namespace Suppliers.IntegrationTests;

public class MediaApiTests(SuppliersApiFactory factory) : ApiTestBase(factory)
{
    // Talks to the S3 gateway directly, as a browser would with the signed URLs.
    private static readonly HttpClient S3 = new(new HttpClientHandler { AllowAutoRedirect = false });

    // Only the leading bytes matter to the server; the rest is filler.
    private static byte[] Png(int size = 256) => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[size - 8]];

    private static byte[] Mp4(int size = 4096) => [0x00, 0x00, 0x00, 0x18, .. "ftypisom"u8.ToArray(), .. new byte[size - 12]];

    private Task<HttpResponseMessage> RequestUploadAsync(Guid supplierId, string fileName, string contentType, long size) =>
        Client.PostAsJsonAsync(
            $"{SuppliersUrl}/{supplierId}/media/uploads", new { fileName, contentType, sizeBytes = size }, Json);

    private async Task<MediaUploadTicket> TicketAsync(Guid supplierId, string fileName, string contentType, long size)
    {
        var response = await RequestUploadAsync(supplierId, fileName, contentType, size);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<MediaUploadTicket>(Json)).ShouldNotBeNull();
    }

    private static async Task PutToS3Async(MediaUploadTicket ticket, byte[] bytes)
    {
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(ticket.Headers["Content-Type"]);
        var response = await S3.PutAsync(ticket.UploadUrl, content);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private Task<HttpResponseMessage> ConfirmAsync(Guid supplierId, Guid mediaId, string fileName, string contentType) =>
        Client.PostAsJsonAsync($"{SuppliersUrl}/{supplierId}/media", new { mediaId, fileName, contentType }, Json);

    /// <summary>The full browser flow: request a signed URL, upload straight to S3, confirm.</summary>
    private async Task<MediaDto> UploadAsync(Guid supplierId, byte[] bytes, string fileName, string contentType)
    {
        var ticket = await TicketAsync(supplierId, fileName, contentType, bytes.Length);
        await PutToS3Async(ticket, bytes);
        var confirm = await ConfirmAsync(supplierId, ticket.MediaId, fileName, contentType);
        confirm.StatusCode.ShouldBe(HttpStatusCode.Created, await confirm.Content.ReadAsStringAsync());
        return (await confirm.Content.ReadFromJsonAsync<MediaDto>(Json)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Uploaded_photo_is_recorded_and_served_from_S3_through_a_redirect()
    {
        var supplier = await CreateAsync(Supplier());
        var bytes = Png();

        var media = await UploadAsync(supplier.Id, bytes, "lodge.png", "image/png");

        media.Kind.ShouldBe(MediaKind.Image);
        media.SizeBytes.ShouldBe(bytes.Length);
        var fetched = await Client.GetFromJsonAsync<SupplierDto>($"{SuppliersUrl}/{supplier.Id}", Json);
        fetched!.Media.ShouldHaveSingleItem().Id.ShouldBe(media.Id);

        var redirect = await Client.GetAsync(media.Url);
        redirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var file = await S3.GetAsync(redirect.Headers.Location);
        file.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await file.Content.ReadAsByteArrayAsync()).ShouldBe(bytes);
    }

    [Fact]
    public async Task Upload_ticket_is_a_signed_PUT_for_the_suppliers_folder()
    {
        var supplier = await CreateAsync(Supplier());

        var ticket = await TicketAsync(supplier.Id, "drive.mp4", "video/mp4", 4096);

        ticket.Method.ShouldBe("PUT");
        ticket.Headers["Content-Type"].ShouldBe("video/mp4");
        ticket.UploadUrl.AbsolutePath.ShouldEndWith($"/suppliers/{supplier.Id}/{ticket.MediaId}.mp4");
        ticket.UploadUrl.Query.ShouldContain("X-Amz-Signature");
        ticket.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("application/pdf", 1000)]
    [InlineData("image/gif", 1000)]
    [InlineData("image/png", SupplierLimits.MaxImageBytes + 1)]
    [InlineData("video/mp4", SupplierLimits.MaxVideoBytes + 1)]
    [InlineData("image/png", 0)]
    public async Task Unsupported_type_or_size_is_rejected_before_any_upload(string contentType, long size)
    {
        var supplier = await CreateAsync(Supplier());

        var response = await RequestUploadAsync(supplier.Id, "file", contentType, size);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("File", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Contents_that_dont_match_the_declared_type_are_rejected_and_deleted()
    {
        var supplier = await CreateAsync(Supplier());
        var ticket = await TicketAsync(supplier.Id, "photo.jpg", "image/jpeg", 64);
        await PutToS3Async(ticket, "MZ this is really an executable"u8.ToArray());

        var confirm = await ConfirmAsync(supplier.Id, ticket.MediaId, "photo.jpg", "image/jpeg");

        confirm.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ConfirmAsync(supplier.Id, ticket.MediaId, "photo.jpg", "image/jpeg"))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest); // The object is gone, so it can't be confirmed later.
        (await Client.GetFromJsonAsync<SupplierDto>($"{SuppliersUrl}/{supplier.Id}", Json))!.Media.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirming_before_uploading_is_rejected()
    {
        var supplier = await CreateAsync(Supplier());
        var ticket = await TicketAsync(supplier.Id, "lodge.png", "image/png", 256);

        var confirm = await ConfirmAsync(supplier.Id, ticket.MediaId, "lodge.png", "image/png");

        confirm.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Confirming_twice_returns_the_same_media_once()
    {
        var supplier = await CreateAsync(Supplier());
        var media = await UploadAsync(supplier.Id, Png(), "lodge.png", "image/png");

        var again = await ConfirmAsync(supplier.Id, media.Id, "lodge.png", "image/png");

        again.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await again.Content.ReadFromJsonAsync<MediaDto>(Json))!.Id.ShouldBe(media.Id);
        (await Client.GetFromJsonAsync<SupplierDto>($"{SuppliersUrl}/{supplier.Id}", Json))!.Media.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Unknown_supplier_returns_404()
    {
        (await RequestUploadAsync(Guid.NewGuid(), "lodge.png", "image/png", 256)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Client.GetAsync($"/api/v1/media/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deleted_media_is_gone_from_the_supplier_and_from_S3()
    {
        var supplier = await CreateAsync(Supplier());
        var media = await UploadAsync(supplier.Id, Mp4(), "drive.mp4", "video/mp4");
        var s3Url = (await Client.GetAsync(media.Url)).Headers.Location;

        var delete = await Client.DeleteAsync($"{SuppliersUrl}/{supplier.Id}/media/{media.Id}");

        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync(media.Url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await S3.GetAsync(s3Url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Client.GetFromJsonAsync<SupplierDto>($"{SuppliersUrl}/{supplier.Id}", Json))!.Media.ShouldBeEmpty();
    }

    [Fact]
    public async Task First_photo_becomes_the_cover_image_in_the_list()
    {
        var supplier = await CreateAsync(Supplier());
        await UploadAsync(supplier.Id, Mp4(), "drive.mp4", "video/mp4");
        var photo = await UploadAsync(supplier.Id, Png(), "cover.png", "image/png");
        await UploadAsync(supplier.Id, Png(), "second.png", "image/png");

        using var json = JsonDocument.Parse(await Client.GetStringAsync(SuppliersUrl));

        json.RootElement.GetProperty("items")[0].GetProperty("coverImageUrl").GetString().ShouldBe(photo.Url);
    }

    [Theory]
    [InlineData("https://app.example", true)]
    [InlineData("https://evil.example", false)]
    public async Task Configured_web_app_origin_may_upload_straight_to_the_bucket(string origin, bool allowed)
    {
        // Starting a host with CorsAllowedOrigins applies the bucket CORS rule, as it does on Render.
        await using var api = Factory.WithWebHostBuilder(b => b.UseSetting("Media:S3:CorsAllowedOrigins:0", "https://app.example"));
        using var client = api.CreateClient();
        // A name the sample data doesn't use: starting the extra host seeds the sample suppliers again.
        var supplier = await CreateAsync(Supplier("CORS Test Lodge"));
        var ticketResponse = await client.PostAsJsonAsync(
            $"{SuppliersUrl}/{supplier.Id}/media/uploads", new { fileName = "lodge.png", contentType = "image/png", sizeBytes = 256 }, Json);
        var ticket = (await ticketResponse.Content.ReadFromJsonAsync<MediaUploadTicket>(Json))!;

        using var preflight = new HttpRequestMessage(HttpMethod.Options, ticket.UploadUrl);
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "PUT");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
        var response = await S3.SendAsync(preflight);

        var allowOrigin = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
        (allowOrigin == origin || allowOrigin == "*").ShouldBe(allowed, $"Access-Control-Allow-Origin: {allowOrigin}");
    }

    [Fact]
    public async Task A_supplier_can_have_at_most_20_photos_and_videos()
    {
        var supplier = await CreateAsync(Supplier());
        for (var i = 0; i < SupplierLimits.MaxMediaPerSupplier; i++)
            await UploadAsync(supplier.Id, Png(64), $"photo-{i}.png", "image/png");

        var response = await RequestUploadAsync(supplier.Id, "one-too-many.png", "image/png", 64);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
