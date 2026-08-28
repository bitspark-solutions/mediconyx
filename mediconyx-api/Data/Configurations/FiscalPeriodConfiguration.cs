using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.FiscalYearId, e.PeriodNumber }).IsUnique();

        entity.Property(e => e.Status).HasConversion<int>();

        entity.HasOne(e => e.FiscalYear)
            .WithMany(e => e.Periods)
            .HasForeignKey(e => e.FiscalYearId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
