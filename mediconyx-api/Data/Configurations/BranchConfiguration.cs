using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.IsActive });
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
        entity.Property(e => e.Address).HasMaxLength(300);
        entity.Property(e => e.City).HasMaxLength(100);
        entity.Property(e => e.Phone).HasMaxLength(30);
        entity.Property(e => e.Email).HasMaxLength(150);
        entity.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
