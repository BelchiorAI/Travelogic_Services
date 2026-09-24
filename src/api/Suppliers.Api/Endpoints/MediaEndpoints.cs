using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.Media;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Api.Endpoints;

internal static class MediaEndpoints
{
    // Largest allowed file plus room for the multipart envelope.
    private const long MaxUploadRequestBytes = SupplierLimits.MaxVideoBytes + 1024 * 1024;

    public static RouteGroupBuilder MapMediaEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapPost("/suppliers/{id:guid}/media", UploadMedia)
            .WithName("UploadSupplierMedia")
            .WithTags("Media")
            .WithSummary("Upload a photo or video to a supplier's profile")
            .WithDescription(
                $"Multipart form with one file in the \"file\" field: {MediaFileType.SupportedFormats}. " +
                $"Images up to {SupplierLimits.MaxImageBytes / (1024 * 1024)} MB, videos up to {SupplierLimits.MaxVideoBytes / (1024 * 1024)} MB, " +
                $"at most {SupplierLimits.MaxMediaPerSupplier} per supplier. The file type is checked from its contents.")
            .Accepts<IFormFile>("multipart/form-data")
            .WithMetadata(new RequestSizeLimitAttribute(MaxUploadRequestBytes))
            // A token-less JSON API: no cookies or browser forms, so antiforgery tokens don't apply.
            .DisableAntiforgery()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);

        v1.MapDelete("/suppliers/{id:guid}/media/{mediaId:guid}", DeleteMedia)
            .WithName("DeleteSupplierMedia")
            .WithTags("Media")
            .WithSummary("Remove a photo or video from a supplier's profile")
            .ProducesProblem(StatusCodes.Status404NotFound);

        v1.MapGet("/media/{mediaId:guid}", GetMediaFile)
            .WithName("GetMediaFile")
            .WithTags("Media")
            .WithSummary("Download a photo or video")
            .WithDescription("Supports range requests, so videos can be streamed and seeked.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return v1;
    }

    private static async Task<Created<MediaDto>> UploadMedia(
        Guid id,
        IFormFile file,
        UploadSupplierMediaHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var media = await handler.HandleAsync(
            new UploadSupplierMediaRequest(id, file.FileName, file.Length, content), cancellationToken);

        return TypedResults.Created($"{httpContext.Request.PathBase}{media.Url}", media);
    }

    private static async Task<NoContent> DeleteMedia(
        Guid id,
        Guid mediaId,
        DeleteSupplierMediaHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, mediaId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<FileStreamHttpResult> GetMediaFile(
        Guid mediaId,
        GetMediaFileHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var file = await handler.HandleAsync(mediaId, cancellationToken);

        // A media id always refers to the same bytes, so browsers and CDNs may cache it for good.
        httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        httpContext.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

        return TypedResults.Stream(file.Content, file.ContentType, lastModified: file.UploadedAt, enableRangeProcessing: true);
    }
}
