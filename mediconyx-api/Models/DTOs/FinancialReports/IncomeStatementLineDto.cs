namespace mediconyx_api.Models.DTOs;

public class IncomeStatementLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
