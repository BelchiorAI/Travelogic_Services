namespace Suppliers.Application.Abstractions;

/// <summary>Port for storing uploaded photo and video files. Implemented in Infrastructure (local disk today, blob storage later).</summary>
public interface IMediaStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken);

    /// <returns>A readable, seekable stream, or <c>null</c> when no file exists for the key.</returns>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
