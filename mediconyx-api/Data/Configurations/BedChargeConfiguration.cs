using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class BedChargeConfiguration : IEntityTypeConfiguration<BedCharge>
{
    public void Configure(EntityTypeBuilder<BedCharge> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.AdmissionId, e.ChargeDate }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.ChargeDate });
        entity.Property(e => e.DailyRate).HasPrecision(12, 2);
        entity.Property(e => e.Amount).HasPrecision(12, 2);
        entity.Property(e => e.Description).HasMaxLength(200);
        entity.HasOne(e => e.Admission)
            .WithMany()
            .HasForeignKey(e => e.AdmissionId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Bed)
            .WithMany()
            .HasForeignKey(e => e.BedId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
