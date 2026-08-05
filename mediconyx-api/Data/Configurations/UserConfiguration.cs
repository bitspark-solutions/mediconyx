using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.Email });
        entity.HasIndex(e => e.Role); // For filtering by role
        entity.HasIndex(e => e.IsActive); // For filtering active/inactive users
        entity.HasIndex(e => e.TenantId); // For tenant-based queries

        entity.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Role)
            .HasConversion<int>();

        entity.HasOne(e => e.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // User has Designation + PrimaryDepartmentId + PrimaryBranchId
        entity.Property(e => e.Designation).HasMaxLength(150);
        entity.HasOne(e => e.PrimaryDepartment)
            .WithMany()
            .HasForeignKey(e => e.PrimaryDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.PrimaryBranch)
            .WithMany()
            .HasForeignKey(e => e.PrimaryBranchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
