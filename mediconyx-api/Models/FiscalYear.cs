namespace mediconyx_api.Models;

/// <summary>
/// A fiscal year for a tenant. Bangladesh default: July 1 – June 30.
/// Each fiscal year contains 12 monthly FiscalPeriods (+ optional period 13 for year-end adjustments).
/// </summary>
public class FiscalYear
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>The calendar year this fiscal year starts in, e.g. 2026 for FY 2026-2027.</summary>
    public int Year { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? ClosedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<FiscalPeriod> Periods { get; set; } = new List<FiscalPeriod>();
}
