using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> entity)
    {
        entity.HasKey(e => e.Id);

        entity.HasIndex(e => e.Email);
        entity.HasIndex(e => e.Token).IsUnique();
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.SalesRequestId).HasFilter("\"SalesRequestId\" IS NOT NULL");
        entity.HasIndex(e => e.TenantId).HasFilter("\"TenantId\" IS NOT NULL");

        entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Token).IsRequired().HasMaxLength(128);
        entity.Property(e => e.PasswordHash).HasMaxLength(500);

        entity.Property(e => e.IntendedRole)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.HasOne(e => e.SalesRequest)
            .WithMany()
            .HasForeignKey(e => e.SalesRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
