using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class SalesRequestConfiguration : IEntityTypeConfiguration<SalesRequest>
{
    public void Configure(EntityTypeBuilder<SalesRequest> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.CreatedAt);
        // Composite indexes for performance
        entity.HasIndex(e => new { e.Status, e.CreatedAt });
        entity.HasIndex(e => e.PaidAt).HasFilter("\"PaidAt\" IS NOT NULL");
        entity.HasIndex(e => new { e.OwnerSalesUserId, e.Status });
        entity.HasIndex(e => new { e.Region, e.Status });
        entity.HasIndex(e => e.Email);
        entity.HasIndex(e => e.TenantId).HasFilter("\"TenantId\" IS NOT NULL");

        entity.Property(e => e.HospitalName).IsRequired().HasMaxLength(300);
        entity.Property(e => e.ContactPerson).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Phone).HasMaxLength(50);
        entity.Property(e => e.Region).HasMaxLength(100);
        entity.Property(e => e.SubscriptionPlan).HasMaxLength(100);
        entity.Property(e => e.Currency).HasMaxLength(10);
        entity.Property(e => e.QuotedPrice).HasColumnType("numeric(18,2)");
        entity.Property(e => e.DiscountApplied).HasColumnType("numeric(5,2)");
        entity.Property(e => e.SpecialTerms).HasMaxLength(2000);

        entity.HasOne(e => e.OwnerSalesUser)
            .WithMany()
            .HasForeignKey(e => e.OwnerSalesUserId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
