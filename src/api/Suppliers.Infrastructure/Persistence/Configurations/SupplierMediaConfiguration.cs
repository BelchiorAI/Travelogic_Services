using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Infrastructure.Persistence.Configurations;

internal sealed class SupplierMediaConfiguration : IEntityTypeConfiguration<SupplierMedia>
{
    public void Configure(EntityTypeBuilder<SupplierMedia> builder)
    {
        builder.ToTable("SupplierMedia");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.FileName).HasMaxLength(SupplierLimits.FileNameMaxLength).IsRequired();
        builder.Property(m => m.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(m => m.StorageKey).HasMaxLength(300).IsRequired();
    }
}
