using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.EntityName, e.EntityId, e.CreatedAt });
        entity.Property(e => e.EntityName).IsRequired().HasMaxLength(200);
        entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
    }
}
