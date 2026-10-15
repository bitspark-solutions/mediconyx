namespace mediconyx_api.Models.DTOs;

/// <summary>Financial reporting DTOs.</summary>

public class TrialBalanceDto
{
    public int FiscalPeriodId { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
}
