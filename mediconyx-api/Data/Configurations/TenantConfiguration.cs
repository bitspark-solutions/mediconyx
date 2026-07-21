using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Name);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);
    }
}
