using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class TechnicalRequirementsConfiguration : IEntityTypeConfiguration<TechnicalRequirements>
{
    public void Configure(EntityTypeBuilder<TechnicalRequirements> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.SalesRequestId).IsUnique();

        entity.HasOne(e => e.SalesRequest)
            .WithOne(r => r.TechnicalRequirements)
            .HasForeignKey<TechnicalRequirements>(e => e.SalesRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
