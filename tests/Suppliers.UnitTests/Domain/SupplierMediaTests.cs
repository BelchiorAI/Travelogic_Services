using Shouldly;
using Suppliers.Application.Suppliers.Media;
using Suppliers.Domain.Common;
using Suppliers.Domain.Suppliers;

namespace Suppliers.UnitTests.Domain;

public class SupplierMediaTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private static Supplier ValidSupplier() =>
        Supplier.Create("Marula Bush Lodge", SupplierType.Accommodation, "Hazyview", "South Africa", Now.AddDays(-1));

    private static SupplierMedia AddPhoto(Supplier supplier, long size = 1000) =>
        supplier.AddMedia(Guid.NewGuid(), MediaKind.Image, "lodge.jpg", "image/jpeg", size, "key", Now);

    [Fact]
    public void AddMedia_adds_it_and_marks_the_supplier_updated()
    {
        var supplier = ValidSupplier();

        var media = AddPhoto(supplier);

        supplier.Media.ShouldHaveSingleItem().ShouldBeSameAs(media);
        media.SupplierId.ShouldBe(supplier.Id);
        supplier.UpdatedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData(MediaKind.Image, SupplierLimits.MaxImageBytes + 1)]
    [InlineData(MediaKind.Video, SupplierLimits.MaxVideoBytes + 1)]
    [InlineData(MediaKind.Image, 0)]
    public void AddMedia_rejects_empty_or_oversized_files(MediaKind kind, long size)
    {
        var supplier = ValidSupplier();

        Should.Throw<DomainException>(() => supplier.AddMedia(Guid.NewGuid(), kind, "f", "x/y", size, "key", Now));
    }

    [Fact]
    public void Videos_may_be_larger_than_images()
    {
        var supplier = ValidSupplier();

        supplier.AddMedia(Guid.NewGuid(), MediaKind.Video, "drive.mp4", "video/mp4", SupplierLimits.MaxVideoBytes, "key", Now)
            .SizeBytes.ShouldBe(SupplierLimits.MaxVideoBytes);
    }

    [Fact]
    public void AddMedia_rejects_the_21st_file()
    {
        var supplier = ValidSupplier();
        for (var i = 0; i < SupplierLimits.MaxMediaPerSupplier; i++)
            AddPhoto(supplier);

        Should.Throw<DomainException>(() => AddPhoto(supplier));
    }

    [Fact]
    public void RemoveMedia_returns_the_removed_item_or_null()
    {
        var supplier = ValidSupplier();
        var media = AddPhoto(supplier);

        supplier.RemoveMedia(Guid.NewGuid(), Now).ShouldBeNull();
        supplier.RemoveMedia(media.Id, Now).ShouldBeSameAs(media);
        supplier.Media.ShouldBeEmpty();
    }
}

public class MediaFileTypeTests
{
    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50 }, "image/webp")]
    [InlineData(new byte[] { 0, 0, 0, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D }, "video/mp4")]
    [InlineData(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x9F }, "video/webm")]
    public void Recognises_supported_formats(byte[] header, string contentType)
    {
        MediaFileType.Detect(header).ShouldNotBeNull().ContentType.ShouldBe(contentType);
    }

    [Theory]
    [InlineData(new byte[] { 0x4D, 0x5A, 0x90, 0x00 })] // Windows executable
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 })] // PDF
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x41, 0x56, 0x45 })] // WAV (RIFF, not WebP)
    [InlineData(new byte[0])]
    public void Rejects_everything_else(byte[] header)
    {
        MediaFileType.Detect(header).ShouldBeNull();
    }
}
