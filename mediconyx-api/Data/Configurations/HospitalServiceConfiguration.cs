using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class HospitalServiceConfiguration : IEntityTypeConfiguration<HospitalService>
{
    public void Configure(EntityTypeBuilder<HospitalService> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.IsActive });
        entity.HasIndex(e => new { e.TenantId, e.ServiceType });
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.ServiceType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.BasePrice).HasColumnType("numeric(10,2)");
    }
}
