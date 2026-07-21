using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class HospitalProfileConfiguration : IEntityTypeConfiguration<HospitalProfile>
{
    public void Configure(EntityTypeBuilder<HospitalProfile> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.SalesRequestId).IsUnique();
        entity.HasIndex(e => e.LicenseNumber);
        entity.HasIndex(e => e.TaxId);

        entity.HasOne(e => e.SalesRequest)
            .WithOne(r => r.HospitalProfile)
            .HasForeignKey<HospitalProfile>(e => e.SalesRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
