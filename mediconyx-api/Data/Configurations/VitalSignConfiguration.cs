using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class VitalSignConfiguration : IEntityTypeConfiguration<VitalSign>
{
    public void Configure(EntityTypeBuilder<VitalSign> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.PatientId, e.RecordedAt });
        entity.HasIndex(e => e.AppointmentId).HasFilter("\"AppointmentId\" IS NOT NULL");
        entity.Property(e => e.TemperatureCelsius).HasColumnType("numeric(4,1)");
        entity.Property(e => e.WeightKg).HasColumnType("numeric(5,1)");
        entity.Property(e => e.HeightCm).HasColumnType("numeric(5,1)");
        entity.Property(e => e.BloodGlucose).HasColumnType("numeric(5,1)");
        entity.Property(e => e.Notes).HasMaxLength(1000);
    }
}
