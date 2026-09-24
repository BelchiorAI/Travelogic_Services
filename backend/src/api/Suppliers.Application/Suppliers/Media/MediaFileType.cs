using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Suppliers.Media;

public sealed record MediaFileType(MediaKind Kind, string ContentType, string Extension)
{
    /// <summary>Bytes needed to recognise every supported format.</summary>
    public const int HeaderLength = 12;

    public const string SupportedFormats = "JPEG, PNG or WebP images and MP4 or WebM videos";

    private static readonly MediaFileType[] Supported =
    [
        new(MediaKind.Image, "image/jpeg", ".jpg"),
        new(MediaKind.Image, "image/png", ".png"),
        new(MediaKind.Image, "image/webp", ".webp"),
        new(MediaKind.Video, "video/mp4", ".mp4"),
        new(MediaKind.Video, "video/webm", ".webm"),
    ];

    /// <summary>The type a client says it will upload; confirmed later from the file's bytes with <see cref="Detect"/>.</summary>
    public static MediaFileType? FromContentType(string? contentType) =>
        Supported.FirstOrDefault(t => string.Equals(t.ContentType, contentType?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> WebmSignature => [0x1A, 0x45, 0xDF, 0xA3];

    /// <summary>
    /// Identifies the file from its first bytes ("magic numbers"), never from the name or the
    /// client's declared content type, so a renamed executable can't be stored as a photo.
    /// </summary>
    public static MediaFileType? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(JpegSignature))
            return new(MediaKind.Image, "image/jpeg", ".jpg");

        if (header.StartsWith(PngSignature))
            return new(MediaKind.Image, "image/png", ".png");

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
            return new(MediaKind.Image, "image/webp", ".webp");

        // MP4 (ISO base media): a box size, then "ftyp".
        if (header.Length >= 8 && header[4..8].SequenceEqual("ftyp"u8))
            return new(MediaKind.Video, "video/mp4", ".mp4");

        // WebM (Matroska/EBML).
        if (header.StartsWith(WebmSignature))
            return new(MediaKind.Video, "video/webm", ".webm");

        return null;
    }
}
