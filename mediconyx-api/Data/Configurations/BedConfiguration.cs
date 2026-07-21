using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.Status });
        entity.HasIndex(e => new { e.TenantId, e.Kind });
        entity.HasIndex(e => new { e.WardId, e.Number }).IsUnique();
        entity.Property(e => e.Number).IsRequired().HasMaxLength(30);
        entity.Property(e => e.Notes).HasMaxLength(300);
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.Kind).HasConversion<int>();
        entity.Property(e => e.CabinType).HasMaxLength(50);
        entity.Property(e => e.Facilities).HasMaxLength(1000);
        entity.HasOne(e => e.Ward)
            .WithMany(w => w.Beds)
            .HasForeignKey(e => e.WardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
