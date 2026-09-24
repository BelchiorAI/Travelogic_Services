using Suppliers.Domain.Common;

namespace Suppliers.Domain.Suppliers;

public enum MediaKind
{
    Image,
    Video,
}

/// <summary>A photo or video on a supplier's profile. The file lives in media storage under <see cref="StorageKey"/>.</summary>
public sealed class SupplierMedia
{
    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public MediaKind Kind { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }

    // For EF Core.
    private SupplierMedia() { }

    internal SupplierMedia(
        Guid id,
        Guid supplierId,
        MediaKind kind,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        DateTimeOffset uploadedAt)
    {
        if (sizeBytes <= 0)
            throw new DomainException("The file is empty.");

        var maxBytes = SupplierLimits.MaxMediaBytes(Guard.Defined(kind, "Media kind"));
        if (sizeBytes > maxBytes)
            throw new DomainException($"{kind} files can be at most {maxBytes / (1024 * 1024)} MB.");

        Id = id;
        SupplierId = supplierId;
        Kind = kind;
        FileName = Guard.Required(fileName, "File name", SupplierLimits.FileNameMaxLength);
        ContentType = Guard.Required(contentType, "Content type", 100);
        SizeBytes = sizeBytes;
        StorageKey = Guard.Required(storageKey, "Storage key", 300);
        UploadedAt = uploadedAt;
    }
}
