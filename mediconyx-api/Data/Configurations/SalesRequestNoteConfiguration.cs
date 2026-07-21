using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class SalesRequestNoteConfiguration : IEntityTypeConfiguration<SalesRequestNote>
{
    public void Configure(EntityTypeBuilder<SalesRequestNote> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.SalesRequestId, e.CreatedAt });
        entity.Property(e => e.Note).IsRequired().HasMaxLength(2000);
        entity.HasOne(e => e.SalesRequest)
            .WithMany(r => r.NotesList)
            .HasForeignKey(e => e.SalesRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.AuthorUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
