namespace mediconyx_api.Models.DTOs;

public class CreateJournalPostingRequest
{
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Memo { get; set; }
}
