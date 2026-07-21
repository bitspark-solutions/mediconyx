using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.IsActive });
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
        entity.Property(e => e.Description).HasMaxLength(500);

        // Department has HeadDoctor
        entity.HasOne(e => e.HeadDoctor)
            .WithMany()
            .HasForeignKey(e => e.HeadDoctorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
