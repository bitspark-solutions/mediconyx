using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.PatientNumber }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.Phone });
        entity.HasIndex(e => new { e.TenantId, e.NationalId })
            .HasFilter("\"NationalId\" IS NOT NULL");
        entity.HasIndex(e => new { e.TenantId, e.IsActive });
        entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Phone).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Email).HasMaxLength(255);
        entity.Property(e => e.NationalId).HasMaxLength(50);
        entity.Property(e => e.PatientNumber).IsRequired().HasMaxLength(20);
        entity.Property(e => e.BloodGroup).HasMaxLength(5);
        entity.Property(e => e.Address).HasMaxLength(500);
        entity.Property(e => e.City).HasMaxLength(100);
        entity.Property(e => e.PostalCode).HasMaxLength(20);
        entity.Property(e => e.EmergencyContactName).HasMaxLength(200);
        entity.Property(e => e.EmergencyContactPhone).HasMaxLength(50);
        entity.Property(e => e.EmergencyContactRelation).HasMaxLength(100);
        entity.Property(e => e.Gender).HasConversion<int>();
    }
}
