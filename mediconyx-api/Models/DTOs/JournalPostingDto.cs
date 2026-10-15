namespace mediconyx_api.Models.DTOs;

/// <summary>DTO for a single debit/credit line.</summary>
public class JournalPostingDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Memo { get; set; }
}
