using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.InvoiceNumber }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.PatientId });
        entity.HasIndex(e => new { e.TenantId, e.Status });
        entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
        entity.HasIndex(e => new { e.TenantId, e.BranchId });
        entity.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(20);
        entity.Property(e => e.LineItems).IsRequired().HasMaxLength(10000);
        entity.Property(e => e.SubTotal).HasColumnType("numeric(12,2)");
        entity.Property(e => e.TaxAmount).HasColumnType("numeric(12,2)");
        entity.Property(e => e.TaxRate).HasColumnType("numeric(5,2)");
        entity.Property(e => e.DiscountAmount).HasColumnType("numeric(12,2)");
        entity.Property(e => e.TotalAmount).HasColumnType("numeric(12,2)");
        entity.Property(e => e.PaidAmount).HasColumnType("numeric(12,2)");
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.PaymentMethod).HasConversion<int>();
        entity.Property(e => e.PaymentReference).HasMaxLength(200);
    }
}
