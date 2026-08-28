using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// An immutable, double-entry journal entry that groups a set of balanced postings.
/// Once IsPosted=true, this record is frozen — no updates or deletes allowed.
/// Corrections are made by creating a reversal entry.
/// </summary>
public class JournalEntry
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>Branch where this entry originated.</summary>
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    /// <summary>
    /// Auto-generated sequential number per tenant, e.g. "JE-20260829-0001".
    /// </summary>
    [Required]
    [StringLength(30)]
    public string EntryNumber { get; set; } = string.Empty;

    /// <summary>
    /// The effective date of the financial event. May differ from CreatedAt
    /// for back-dated adjustments (within open periods only).
    /// </summary>
    public DateTime EntryDate { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>What generated this entry — manual, invoice, bed charge, etc.</summary>
    public JournalEntrySourceType SourceType { get; set; } = JournalEntrySourceType.Manual;

    /// <summary>FK to the source record (e.g. Invoice.Id, BedCharge.Id). Null for manual entries.</summary>
    public int? SourceId { get; set; }

    /// <summary>Whether this entry has been reversed by another entry.</summary>
    public bool IsReversed { get; set; }

    /// <summary>The entry that reversed this one, if applicable.</summary>
    public int? ReversedByEntryId { get; set; }

    /// <summary>The fiscal period this entry belongs to. Entries cannot post to closed periods.</summary>
    public int FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;

    /// <summary>
    /// Draft entries can be edited. Once posted, the entry is immutable.
    /// </summary>
    public bool IsPosted { get; set; }

    public int CreatedByUserId { get; set; }

    /// <summary>Immutable — never updated after creation.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation — the debit/credit lines
    public ICollection<JournalPosting> Postings { get; set; } = new List<JournalPosting>();
}
