using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.EntryNumber }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.EntryDate });
        entity.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceId });
        entity.HasIndex(e => new { e.TenantId, e.FiscalPeriodId });

        entity.Property(e => e.EntryNumber).IsRequired().HasMaxLength(30);
        entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
        entity.Property(e => e.SourceType).HasConversion<int>();

        entity.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.FiscalPeriod)
            .WithMany(e => e.JournalEntries)
            .HasForeignKey(e => e.FiscalPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
