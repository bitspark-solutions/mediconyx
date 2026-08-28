using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A single debit or credit line within a JournalEntry.
/// Every JournalEntry must have at least two postings where SUM(Debit) = SUM(Credit).
/// Exactly one of DebitAmount/CreditAmount must be > 0 per row.
/// </summary>
public class JournalPosting
{
    public int Id { get; set; }

    public int JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    /// <summary>
    /// The debit amount for this posting. Zero if this is a credit line.
    /// Stored as decimal(19,4) for precision.
    /// </summary>
    public decimal DebitAmount { get; set; }

    /// <summary>
    /// The credit amount for this posting. Zero if this is a debit line.
    /// Stored as decimal(19,4) for precision.
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>Optional line-level memo, e.g. "Payment for INV-20260829-0001".</summary>
    [StringLength(300)]
    public string? Memo { get; set; }
}
