using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class JournalPostingConfiguration : IEntityTypeConfiguration<JournalPosting>
{
    public void Configure(EntityTypeBuilder<JournalPosting> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.JournalEntryId);
        entity.HasIndex(e => e.AccountId);

        entity.Property(e => e.DebitAmount).HasColumnType("numeric(19,4)");
        entity.Property(e => e.CreditAmount).HasColumnType("numeric(19,4)");
        entity.Property(e => e.Memo).HasMaxLength(300);

        entity.HasOne(e => e.JournalEntry)
            .WithMany(e => e.Postings)
            .HasForeignKey(e => e.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Account)
            .WithMany(e => e.Postings)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
