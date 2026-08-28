using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.InvoiceId);
        entity.HasIndex(e => new { e.TenantId, e.ServiceType });

        entity.Property(e => e.Description).IsRequired().HasMaxLength(300);
        entity.Property(e => e.ServiceType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Quantity).HasColumnType("numeric(10,2)");
        entity.Property(e => e.UnitPrice).HasColumnType("numeric(12,2)");
        entity.Property(e => e.Amount).HasColumnType("numeric(12,2)");
        entity.Property(e => e.DiscountAmount).HasColumnType("numeric(12,2)");
        entity.Property(e => e.TaxAmount).HasColumnType("numeric(12,2)");
        entity.Property(e => e.NetAmount).HasColumnType("numeric(12,2)");

        entity.HasOne(e => e.Invoice)
            .WithMany()
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.RevenueAccount)
            .WithMany()
            .HasForeignKey(e => e.RevenueAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
