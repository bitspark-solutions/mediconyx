using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.IsActive });
        entity.HasIndex(e => new { e.TenantId, e.WardType });
        entity.HasIndex(e => new { e.TenantId, e.BranchId });
        // Ward codes are unique per branch (Stage 2: was per-tenant before branches).
        entity.HasIndex(e => new { e.TenantId, e.BranchId, e.Code }).IsUnique();
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
        entity.Property(e => e.WardType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Location).HasMaxLength(200);
        entity.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
