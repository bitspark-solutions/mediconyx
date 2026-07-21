using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class TelehealthSessionConfiguration : IEntityTypeConfiguration<TelehealthSession>
{
    public void Configure(EntityTypeBuilder<TelehealthSession> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.Status });
        entity.HasIndex(e => new { e.DoctorId, e.ScheduledAt });
        entity.HasIndex(e => new { e.PatientId, e.ScheduledAt });
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.JoinUrl).HasMaxLength(500);
        entity.Property(e => e.JoinToken).HasMaxLength(100);
        entity.Property(e => e.Notes).HasMaxLength(2000);
        entity.HasOne(e => e.Patient)
            .WithMany()
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Doctor)
            .WithMany()
            .HasForeignKey(e => e.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
