using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class DischargeSummaryConfiguration : IEntityTypeConfiguration<DischargeSummary>
{
    public void Configure(EntityTypeBuilder<DischargeSummary> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.AdmissionId).IsUnique();
        entity.Property(e => e.ConditionAtDischarge).HasConversion<int>();
        entity.Property(e => e.DischargeInstructions).HasMaxLength(2000);
        entity.Property(e => e.MedicationsOnDischarge).HasMaxLength(2000);
        entity.HasOne(e => e.Admission)
            .WithOne(a => a.DischargeSummary)
            .HasForeignKey<DischargeSummary>(e => e.AdmissionId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.DischargingDoctor)
            .WithMany()
            .HasForeignKey(e => e.DischargingDoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
