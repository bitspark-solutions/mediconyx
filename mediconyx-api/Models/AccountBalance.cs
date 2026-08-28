namespace mediconyx_api.Models;

/// <summary>
/// Materialized running balance for an account within a fiscal period.
/// Updated by LedgerService after each journal entry posting.
/// Avoids expensive SUM(postings) queries for financial reporting.
/// </summary>
public class AccountBalance
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public int FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;

    /// <summary>Debit balance carried forward from the previous period's close.</summary>
    public decimal OpeningDebit { get; set; }

    /// <summary>Credit balance carried forward from the previous period's close.</summary>
    public decimal OpeningCredit { get; set; }

    /// <summary>Sum of all debit postings in this period.</summary>
    public decimal PeriodDebit { get; set; }

    /// <summary>Sum of all credit postings in this period.</summary>
    public decimal PeriodCredit { get; set; }

    /// <summary>OpeningDebit + PeriodDebit.</summary>
    public decimal ClosingDebit { get; set; }

    /// <summary>OpeningCredit + PeriodCredit.</summary>
    public decimal ClosingCredit { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
