using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.PatientId });
        entity.HasIndex(e => new { e.TenantId, e.DoctorId });
        entity.HasIndex(e => e.AppointmentId);
        entity.Property(e => e.Medications).IsRequired().HasMaxLength(5000);
        entity.Property(e => e.Instructions).HasMaxLength(2000);
        entity.Property(e => e.Allergies).HasMaxLength(1000);
    }
}
