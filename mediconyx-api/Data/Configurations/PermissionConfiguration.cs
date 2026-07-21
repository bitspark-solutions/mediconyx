using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Name).IsUnique();
        entity.HasIndex(e => e.Category);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Description).HasMaxLength(300);
    }
}
