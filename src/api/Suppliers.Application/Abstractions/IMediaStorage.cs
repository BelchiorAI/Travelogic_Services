namespace Suppliers.Application.Abstractions;

/// <summary>A short-lived URL the browser uploads a file to directly, with the headers it must send.</summary>
public sealed record PresignedUpload(Uri Url, string Method, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt);

/// <summary>
/// Port for the object store that holds photo and video files (AWS S3, or an S3-compatible store locally). The database keeps
/// only each file's key; the bytes travel directly between the browser and the store via signed URLs.
/// </summary>
public interface IMediaStorage
{
    PresignedUpload CreateUploadUrl(string key, string contentType, TimeSpan lifetime);

    Uri CreateDownloadUrl(string key, TimeSpan lifetime);

    /// <returns>The object's size in bytes, or <c>null</c> when nothing has been uploaded under the key.</returns>
    Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken);

    /// <summary>Reads up to <paramref name="length"/> bytes from the start of the object, to identify its type.</summary>
    Task<byte[]> ReadStartAsync(string key, int length, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
