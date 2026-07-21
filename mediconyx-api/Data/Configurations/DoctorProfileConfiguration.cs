using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class DoctorProfileConfiguration : IEntityTypeConfiguration<DoctorProfile>
{
    public void Configure(EntityTypeBuilder<DoctorProfile> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.IsActive });
        entity.HasIndex(e => new { e.TenantId, e.Specialization });
        entity.HasIndex(e => e.UserId).IsUnique();
        entity.Property(e => e.MedicalLicenseNumber).HasMaxLength(100);
        entity.Property(e => e.Specialization).IsRequired().HasMaxLength(100);
        entity.Property(e => e.ConsultationFee).HasColumnType("numeric(10,2)");
    }
}
