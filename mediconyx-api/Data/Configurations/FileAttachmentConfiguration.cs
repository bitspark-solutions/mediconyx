using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.PatientId });
        entity.HasIndex(e => e.StorageFileId);
        entity.HasIndex(e => new { e.TenantId, e.Category });
        entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Category).HasConversion<int>();
        entity.Property(e => e.Description).HasMaxLength(1000);
    }
}
