namespace mediconyx_api.Models.DTOs;

/// <summary>DTO for a journal entry with its postings.</summary>
public class JournalEntryDto
{
    public int Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public int? SourceId { get; set; }
    public bool IsReversed { get; set; }
    public int? ReversedByEntryId { get; set; }
    public bool IsPosted { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public int FiscalPeriodId { get; set; }
    public string? FiscalPeriodLabel { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<JournalPostingDto> Postings { get; set; } = new();
}
