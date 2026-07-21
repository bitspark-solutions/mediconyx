using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class OnboardingTimelineEventConfiguration : IEntityTypeConfiguration<OnboardingTimelineEvent>
{
    public void Configure(EntityTypeBuilder<OnboardingTimelineEvent> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.SalesRequestId, e.HappenedAt });
        entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
        entity.HasOne(e => e.SalesRequest)
            .WithMany(r => r.Timeline)
            .HasForeignKey(e => e.SalesRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
