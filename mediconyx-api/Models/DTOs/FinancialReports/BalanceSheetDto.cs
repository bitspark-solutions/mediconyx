namespace mediconyx_api.Models.DTOs;

public class BalanceSheetDto
{
    public DateTime AsOfDate { get; set; }
    public BalanceSheetSectionDto Assets { get; set; } = new();
    public BalanceSheetSectionDto Liabilities { get; set; } = new();
    public BalanceSheetSectionDto Equity { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilitiesAndEquity { get; set; }
    public bool IsBalanced { get; set; }
}
