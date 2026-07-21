using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class ModuleSelectionConfiguration : IEntityTypeConfiguration<ModuleSelection>
{
    public void Configure(EntityTypeBuilder<ModuleSelection> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.SalesRequestId).IsUnique();

        entity.HasOne(e => e.SalesRequest)
            .WithOne(r => r.ModuleSelection)
            .HasForeignKey<ModuleSelection>(e => e.SalesRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
