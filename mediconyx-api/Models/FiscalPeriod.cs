namespace mediconyx_api.Models;

/// <summary>
/// A monthly accounting period within a fiscal year.
/// Controls whether journal entries can be posted (Open) or are frozen (Closed/Locked).
/// Period 13 is optional and used for year-end adjustment entries.
/// </summary>
public class FiscalPeriod
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public int FiscalYearId { get; set; }
    public FiscalYear FiscalYear { get; set; } = null!;

    /// <summary>1-12 for standard months, 13 for year-end adjustments.</summary>
    public int PeriodNumber { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public FiscalPeriodStatus Status { get; set; } = FiscalPeriodStatus.Open;

    public int? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    public ICollection<AccountBalance> AccountBalances { get; set; } = new List<AccountBalance>();
}
