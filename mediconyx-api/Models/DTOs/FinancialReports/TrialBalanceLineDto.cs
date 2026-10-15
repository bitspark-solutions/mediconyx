namespace mediconyx_api.Models.DTOs;

public class TrialBalanceLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
}
