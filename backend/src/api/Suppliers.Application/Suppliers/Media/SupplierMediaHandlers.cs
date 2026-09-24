using FluentValidation;
using FluentValidation.Results;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Common;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Domain.Common;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Suppliers.Media;

// Uploads happen in three steps so large files never pass through the API:
//   1. RequestMediaUpload: check the declared file, return a signed URL to upload it to directly.
//   2. The browser PUTs the file to that URL (AWS S3, or an S3-compatible store locally).
//   3. ConfirmMediaUpload: verify what actually arrived, then save its reference in the database.

public sealed record RequestMediaUploadRequest(Guid SupplierId, string FileName, string ContentType, long SizeBytes);

/// <param name="MediaId">Send back to the confirm step once the upload has finished.</param>
public sealed record MediaUploadTicket(
    Guid MediaId,
    Uri UploadUrl,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAt);

public sealed record ConfirmMediaUploadRequest(Guid MediaId, string FileName, string ContentType);

internal static class MediaUploads
{
    public static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(15);

    public static string KeyFor(Guid supplierId, Guid mediaId, MediaFileType type) =>
        $"suppliers/{supplierId}/{mediaId}{type.Extension}";

    public static MediaFileType RequireSupportedType(string contentType) =>
        MediaFileType.FromContentType(contentType)
        ?? throw FileError($"Unsupported file. Upload {MediaFileType.SupportedFormats}.");

    public static ValidationException FileError(string message) => new([new ValidationFailure("File", message)]);
}

public sealed class RequestMediaUploadHandler(ISupplierRepository repository, IMediaStorage storage)
{
    public async Task<MediaUploadTicket> HandleAsync(RequestMediaUploadRequest request, CancellationToken cancellationToken)
    {
        var supplier = await repository.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new NotFoundException("Supplier not found.");

        var type = MediaUploads.RequireSupportedType(request.ContentType);

        var maxBytes = SupplierLimits.MaxMediaBytes(type.Kind);
        if (request.SizeBytes <= 0 || request.SizeBytes > maxBytes)
            throw MediaUploads.FileError($"{type.Kind} files must be between 1 byte and {maxBytes / (1024 * 1024)} MB.");

        if (supplier.Media.Count >= SupplierLimits.MaxMediaPerSupplier)
            throw MediaUploads.FileError($"A supplier can have at most {SupplierLimits.MaxMediaPerSupplier} photos and videos.");

        var mediaId = Guid.CreateVersion7();
        var upload = storage.CreateUploadUrl(
            MediaUploads.KeyFor(supplier.Id, mediaId, type), type.ContentType, MediaUploads.UploadUrlLifetime);

        return new MediaUploadTicket(mediaId, upload.Url, upload.Method, upload.Headers, upload.ExpiresAt);
    }
}

public sealed class ConfirmMediaUploadHandler(
    ISupplierRepository repository,
    IMediaStorage storage,
    TimeProvider timeProvider)
{
    public async Task<MediaDto> HandleAsync(Guid supplierId, ConfirmMediaUploadRequest request, CancellationToken cancellationToken)
    {
        var supplier = await repository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundException("Supplier not found.");

        // Confirming twice (e.g. a retried request) returns the same result.
        if (supplier.Media.FirstOrDefault(m => m.Id == request.MediaId) is { } existing)
            return existing.ToDto();

        var declared = MediaUploads.RequireSupportedType(request.ContentType);
        var key = MediaUploads.KeyFor(supplier.Id, request.MediaId, declared);

        var size = await storage.GetSizeAsync(key, cancellationToken)
            ?? throw MediaUploads.FileError("The upload wasn't found. Upload the file first, or request a new upload link.");

        // Trust the bytes, not the declared type: a renamed executable must not become a "photo".
        var header = await storage.ReadStartAsync(key, MediaFileType.HeaderLength, cancellationToken);
        if (MediaFileType.Detect(header)?.ContentType != declared.ContentType)
        {
            await storage.DeleteAsync(key, CancellationToken.None);
            throw MediaUploads.FileError($"The file's contents are not a valid {declared.ContentType} file. Upload {MediaFileType.SupportedFormats}.");
        }

        SupplierMedia media;
        try
        {
            // The domain re-checks the real size and the per-supplier limit.
            media = supplier.AddMedia(
                request.MediaId, declared.Kind, CleanFileName(request.FileName, declared.Extension),
                declared.ContentType, size, key, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is DomainException or ConflictException)
        {
            // Nothing references the object, so don't keep it.
            await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }

        return media.ToDto();
    }

    /// <summary>Keeps only the name part (no client paths) and a sensible length; the name is display-only.</summary>
    private static string CleanFileName(string? fileName, string extension)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (name.Length == 0)
            return "upload" + extension;

        return name.Length <= SupplierLimits.FileNameMaxLength
            ? name
            : name[..(SupplierLimits.FileNameMaxLength - extension.Length)] + extension;
    }
}

public sealed class DeleteSupplierMediaHandler(
    ISupplierRepository repository,
    IMediaStorage storage,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(Guid supplierId, Guid mediaId, CancellationToken cancellationToken)
    {
        var supplier = await repository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundException("Supplier not found.");

        var media = supplier.RemoveMedia(mediaId, timeProvider.GetUtcNow())
            ?? throw new NotFoundException("This supplier has no photo or video with that id.");

        await repository.SaveChangesAsync(cancellationToken);

        // After the database change: if this fails, only an unreferenced object is left behind.
        await storage.DeleteAsync(media.StorageKey, cancellationToken);
    }
}

/// <summary>Turns the stable media URL into a short-lived signed URL on the object store.</summary>
public sealed class GetMediaDownloadUrlHandler(ISupplierQueries queries, IMediaStorage storage)
{
    public static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromHours(1);

    public async Task<Uri> HandleAsync(Guid mediaId, CancellationToken cancellationToken)
    {
        var info = await queries.GetMediaFileInfoAsync(mediaId, cancellationToken)
            ?? throw new NotFoundException("Photo or video not found.");

        return storage.CreateDownloadUrl(info.StorageKey, DownloadUrlLifetime);
    }
}
