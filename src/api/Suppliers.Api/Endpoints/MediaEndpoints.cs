using Microsoft.AspNetCore.Http.HttpResults;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.Media;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Api.Endpoints;

internal static class MediaEndpoints
{
    public static RouteGroupBuilder MapMediaEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapPost("/suppliers/{id:guid}/media/uploads", RequestUpload)
            .WithName("RequestSupplierMediaUpload")
            .WithTags("Media")
            .WithSummary("Step 1 of 3: get a signed URL to upload a photo or video to")
            .WithDescription(
                $"Checks the declared file ({MediaFileType.SupportedFormats}; images up to " +
                $"{SupplierLimits.MaxImageBytes / (1024 * 1024)} MB, videos up to {SupplierLimits.MaxVideoBytes / (1024 * 1024)} MB, " +
                $"at most {SupplierLimits.MaxMediaPerSupplier} per supplier) and returns a URL valid for 15 minutes. " +
                "Step 2: send the file to uploadUrl with the given method and headers. " +
                "Step 3: POST /suppliers/{id}/media with the mediaId.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        v1.MapPost("/suppliers/{id:guid}/media", ConfirmUpload)
            .WithName("ConfirmSupplierMediaUpload")
            .WithTags("Media")
            .WithSummary("Step 3 of 3: confirm an uploaded photo or video and add it to the supplier")
            .WithDescription("Verifies the uploaded file's real type and size, then records it. Safe to retry.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        v1.MapDelete("/suppliers/{id:guid}/media/{mediaId:guid}", DeleteMedia)
            .WithName("DeleteSupplierMedia")
            .WithTags("Media")
            .WithSummary("Remove a photo or video from a supplier's profile")
            .ProducesProblem(StatusCodes.Status404NotFound);

        v1.MapGet("/media/{mediaId:guid}", GetMedia)
            .WithName("GetMediaFile")
            .WithTags("Media")
            .WithSummary("View a photo or video")
            .WithDescription("Redirects to a short-lived signed URL on the object store, which supports range requests for video.")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return v1;
    }

    /// <summary>What the client plans to upload; the real type and size are checked when it confirms.</summary>
    internal sealed record RequestUploadBody(string FileName, string ContentType, long SizeBytes);

    private static async Task<Ok<MediaUploadTicket>> RequestUpload(
        Guid id,
        RequestUploadBody body,
        RequestMediaUploadHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(
            new RequestMediaUploadRequest(id, body.FileName, body.ContentType, body.SizeBytes), cancellationToken));

    private static async Task<Created<MediaDto>> ConfirmUpload(
        Guid id,
        ConfirmMediaUploadRequest body,
        ConfirmMediaUploadHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var media = await handler.HandleAsync(id, body, cancellationToken);
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

    private static async Task<RedirectHttpResult> GetMedia(
        Guid mediaId,
        GetMediaDownloadUrlHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var url = await handler.HandleAsync(mediaId, cancellationToken);

        // The signed URL expires, so browsers may reuse this redirect only while it is still valid.
        var maxAge = (int)(GetMediaDownloadUrlHandler.DownloadUrlLifetime - TimeSpan.FromMinutes(5)).TotalSeconds;
        httpContext.Response.Headers.CacheControl = $"private, max-age={maxAge}";

        return TypedResults.Redirect(url.ToString());
    }
}
