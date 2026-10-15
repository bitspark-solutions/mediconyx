namespace mediconyx_api.Models.DTOs;

public class CreateJournalEntryRequest
{
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public JournalEntrySourceType SourceType { get; set; } = JournalEntrySourceType.Manual;
    public int? SourceId { get; set; }
    public List<CreateJournalPostingRequest> Postings { get; set; } = new();
}
