using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class MedicalNoteConfiguration : IEntityTypeConfiguration<MedicalNote>
{
    public void Configure(EntityTypeBuilder<MedicalNote> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.PatientId });
        entity.HasIndex(e => new { e.TenantId, e.AuthorUserId });
        entity.HasIndex(e => e.AppointmentId).HasFilter("\"AppointmentId\" IS NOT NULL");
        entity.Property(e => e.NoteType).HasConversion<int>();
        entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
        entity.Property(e => e.Diagnosis).HasMaxLength(500);
    }
}
