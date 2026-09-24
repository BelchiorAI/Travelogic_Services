using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(SupplierLimits.NameMaxLength).IsRequired();
        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(SupplierLimits.DescriptionMaxLength);
        builder.Property(s => s.ContactName).HasMaxLength(SupplierLimits.ContactMaxLength);
        builder.Property(s => s.Email).HasMaxLength(SupplierLimits.ContactMaxLength);
        builder.Property(s => s.Phone).HasMaxLength(SupplierLimits.PhoneMaxLength);
        builder.Property(s => s.Website).HasMaxLength(SupplierLimits.WebsiteMaxLength);
        builder.Property(s => s.AddressLine).HasMaxLength(SupplierLimits.AddressLineMaxLength);
        builder.Property(s => s.City).HasMaxLength(SupplierLimits.CityMaxLength).IsRequired();
        builder.Property(s => s.Country).HasMaxLength(SupplierLimits.CountryMaxLength).IsRequired();
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.Type);
        builder.HasIndex(s => new { s.Name, s.City }).IsUnique();

        builder.HasMany(s => s.Services)
            .WithOne()
            .HasForeignKey(s => s.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Services)
            .HasField("_services")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Media)
            .WithOne()
            .HasForeignKey(m => m.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Media)
            .HasField("_media")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
