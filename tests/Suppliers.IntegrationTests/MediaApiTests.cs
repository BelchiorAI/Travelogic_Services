using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Domain.Suppliers;
using Suppliers.IntegrationTests.Infrastructure;

namespace Suppliers.IntegrationTests;

public class MediaApiTests(SuppliersApiFactory factory) : ApiTestBase(factory)
{
    // Only the leading bytes matter to the server; the rest is filler.
    private static byte[] Png(int size = 256) => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[size - 8]];

    private static byte[] Mp4(int size = 4096) => [0x00, 0x00, 0x00, 0x18, .. "ftypisom"u8.ToArray(), .. new byte[size - 12]];

    private Task<HttpResponseMessage> UploadAsync(Guid supplierId, byte[] bytes, string fileName, string declaredType = "application/octet-stream")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(declaredType);
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        return Client.PostAsync($"{SuppliersUrl}/{supplierId}/media", form);
    }

    private async Task<MediaDto> UploadOkAsync(Guid supplierId, byte[] bytes, string fileName)
    {
        var response = await UploadAsync(supplierId, bytes, fileName);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<MediaDto>(Json)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Uploaded_photo_is_listed_on_the_supplier_and_can_be_downloaded()
    {
        var supplier = await CreateAsync(Supplier());
        var bytes = Png();

        var response = await UploadAsync(supplier.Id, bytes, "lodge.png");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var media = (await response.Content.ReadFromJsonAsync<MediaDto>(Json))!;
        media.Kind.ShouldBe(MediaKind.Image);
        media.ContentType.ShouldBe("image/png");
        media.FileName.ShouldBe("lodge.png");
        media.SizeBytes.ShouldBe(bytes.Length);
        response.Headers.Location!.ToString().ShouldBe(media.Url);

        var fetched = await Client.GetFromJsonAsync<SupplierDto>($"{SuppliersUrl}/{supplier.Id}", Json);
        fetched!.Media.ShouldHaveSingleItem().Id.ShouldBe(media.Id);

        var file = await Client.GetAsync(media.Url);
        file.StatusCode.ShouldBe(HttpStatusCode.OK);
        file.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        (await file.Content.ReadAsByteArrayAsync()).ShouldBe(bytes);
    }

    [Fact]
    public async Task File_type_comes_from_the_contents_not_the_name_or_declared_type()
    {
        var supplier = await CreateAsync(Supplier());

        // An MP4 named like a photo and declared as one is still stored as a video.
        var media = await UploadOkAsync(supplier.Id, Mp4(), "holiday.jpg");

        media.Kind.ShouldBe(MediaKind.Video);
        media.ContentType.ShouldBe("video/mp4");
    }

    [Fact]
    public async Task Unsupported_file_returns_400_with_a_File_error()
    {
        var supplier = await CreateAsync(Supplier());

        var response = await UploadAsync(supplier.Id, "MZ this is not a photo"u8.ToArray(), "photo.jpg", "image/jpeg");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("File", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Upload_to_an_unknown_supplier_returns_404()
    {
        var response = await UploadAsync(Guid.NewGuid(), Png(), "lodge.png");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Video_supports_range_requests_for_streaming()
    {
        var supplier = await CreateAsync(Supplier());
        var bytes = Mp4();
        var media = await UploadOkAsync(supplier.Id, bytes, "drive.mp4");

        using var request = new HttpRequestMessage(HttpMethod.Get, media.Url);
        request.Headers.Range = new RangeHeaderValue(100, 199);
        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(bytes[100..200]);
    }

    [Fact]
    public async Task Deleted_media_is_gone_from_the_supplier_and_the_file_store()
    {
        var supplier = await CreateAsync(Supplier());
        var media = await UploadOkAsync(supplier.Id, Png(), "lodge.png");

        var delete = await Client.DeleteAsync($"{SuppliersUrl}/{supplier.Id}/media/{media.Id}");

        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync(media.Url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var fetched = await Client.GetFromJsonAsync<SupplierDto>($"{SuppliersUrl}/{supplier.Id}", Json);
        fetched!.Media.ShouldBeEmpty();
        (await Client.DeleteAsync($"{SuppliersUrl}/{supplier.Id}/media/{media.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task First_photo_becomes_the_cover_image_in_the_list()
    {
        var supplier = await CreateAsync(Supplier());
        await UploadOkAsync(supplier.Id, Mp4(), "drive.mp4");
        var photo = await UploadOkAsync(supplier.Id, Png(), "cover.png");
        await UploadOkAsync(supplier.Id, Png(), "second.png");

        using var json = JsonDocument.Parse(await Client.GetStringAsync(SuppliersUrl));

        json.RootElement.GetProperty("items")[0].GetProperty("coverImageUrl").GetString().ShouldBe(photo.Url);
    }

    [Fact]
    public async Task A_supplier_can_have_at_most_20_photos_and_videos()
    {
        var supplier = await CreateAsync(Supplier());
        for (var i = 0; i < SupplierLimits.MaxMediaPerSupplier; i++)
            await UploadOkAsync(supplier.Id, Png(64), $"photo-{i}.png");

        var response = await UploadAsync(supplier.Id, Png(64), "one-too-many.png");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
