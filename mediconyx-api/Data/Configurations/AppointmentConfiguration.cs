using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.AppointmentDate, e.DoctorId });
        entity.HasIndex(e => new { e.TenantId, e.PatientId });
        entity.HasIndex(e => new { e.TenantId, e.Status });
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.Type).HasConversion<int>();
        entity.Property(e => e.Reason).HasMaxLength(500);
        entity.Property(e => e.Notes).HasMaxLength(2000);
        entity.Property(e => e.CancelReason).HasMaxLength(500);
    }
}
