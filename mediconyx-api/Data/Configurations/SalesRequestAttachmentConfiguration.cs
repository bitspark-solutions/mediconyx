using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class SalesRequestAttachmentConfiguration : IEntityTypeConfiguration<SalesRequestAttachment>
{
    public void Configure(EntityTypeBuilder<SalesRequestAttachment> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.SalesRequestId);
        entity.HasIndex(e => e.Sha256);
        entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);
        entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Sha256).IsRequired().HasMaxLength(64);
        entity.HasOne(e => e.SalesRequest)
            .WithMany(r => r.Attachments)
            .HasForeignKey(e => e.SalesRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
