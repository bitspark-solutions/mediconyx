using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class AdmissionConfiguration : IEntityTypeConfiguration<Admission>
{
    public void Configure(EntityTypeBuilder<Admission> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.Status });
        entity.HasIndex(e => new { e.TenantId, e.PatientId, e.Status });
        entity.HasIndex(e => new { e.TenantId, e.BedId });
        entity.Property(e => e.AdmissionType).HasConversion<int>();
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.Reason).HasMaxLength(1000);
        entity.Property(e => e.DiagnosisOnAdmission).HasMaxLength(2000);
        entity.Property(e => e.DischargeNotes).HasMaxLength(2000);
        entity.HasOne(e => e.Patient)
            .WithMany()
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Bed)
            .WithMany()
            .HasForeignKey(e => e.BedId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.AttendingDoctor)
            .WithMany()
            .HasForeignKey(e => e.AttendingDoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
