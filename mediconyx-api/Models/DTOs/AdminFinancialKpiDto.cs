namespace mediconyx_api.Models.DTOs;

/// <summary>Admin executive dashboard KPI DTOs.</summary>

public class AdminFinancialKpiDto
{
    public decimal TotalRevenue { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal NetOperatingProfit { get; set; }
    public decimal OperatingMarginPercent { get; set; }
    public decimal AccountsReceivable { get; set; }
    public decimal ArDays { get; set; }
    public decimal CashOnHand { get; set; }
    public string DateRangeLabel { get; set; } = string.Empty;
    public List<DepartmentalRevenueDto> DepartmentalRevenue { get; set; } = new();
}
