namespace mediconyx_api.Models.DTOs;

public class BalanceSheetLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public List<BalanceSheetLineDto> Children { get; set; } = new();
}
