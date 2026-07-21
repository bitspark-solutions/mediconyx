using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class AdmissionTransferConfiguration : IEntityTypeConfiguration<AdmissionTransfer>
{
    public void Configure(EntityTypeBuilder<AdmissionTransfer> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.AdmissionId, e.TransferredAt });
        entity.Property(e => e.Reason).HasMaxLength(500);
        entity.HasOne(e => e.Admission)
            .WithMany(a => a.Transfers)
            .HasForeignKey(e => e.AdmissionId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.FromBed)
            .WithMany()
            .HasForeignKey(e => e.FromBedId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.ToBed)
            .WithMany()
            .HasForeignKey(e => e.ToBedId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
