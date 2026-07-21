using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class AIMessageConfiguration : IEntityTypeConfiguration<AIMessage>
{
    public void Configure(EntityTypeBuilder<AIMessage> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.ConversationId);
        entity.HasIndex(e => e.CreatedAt);
        entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
        entity.Property(e => e.Role).HasConversion<int>();
        entity.Property(e => e.Confidence).HasColumnType("numeric(3,2)");
    }
}
