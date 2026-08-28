using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Configurations;

public class AccountBalanceConfiguration : IEntityTypeConfiguration<AccountBalance>
{
    public void Configure(EntityTypeBuilder<AccountBalance> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TenantId, e.AccountId, e.FiscalPeriodId }).IsUnique();

        entity.Property(e => e.OpeningDebit).HasColumnType("numeric(19,4)");
        entity.Property(e => e.OpeningCredit).HasColumnType("numeric(19,4)");
        entity.Property(e => e.PeriodDebit).HasColumnType("numeric(19,4)");
        entity.Property(e => e.PeriodCredit).HasColumnType("numeric(19,4)");
        entity.Property(e => e.ClosingDebit).HasColumnType("numeric(19,4)");
        entity.Property(e => e.ClosingCredit).HasColumnType("numeric(19,4)");

        entity.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.FiscalPeriod)
            .WithMany(e => e.AccountBalances)
            .HasForeignKey(e => e.FiscalPeriodId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
