using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Infrastructure.Persistence.Configurations;

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(SupplierLimits.NameMaxLength).IsRequired();
        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(SupplierLimits.DescriptionMaxLength);
        builder.Property(s => s.Price).HasPrecision(18, 2);
        builder.Property(s => s.Currency).HasMaxLength(SupplierLimits.CurrencyLength).IsFixedLength().IsRequired();
        builder.Property(s => s.PricingUnit).HasConversion<string>().HasMaxLength(30).IsRequired();
    }
}
