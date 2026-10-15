namespace mediconyx_api.Models.DTOs;

public class IncomeStatementDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public IncomeStatementSectionDto Revenue { get; set; } = new();
    public IncomeStatementSectionDto ContraRevenue { get; set; } = new();
    public decimal NetRevenue { get; set; }
    public IncomeStatementSectionDto Expenses { get; set; } = new();
    public decimal NetIncome { get; set; }
    public decimal NetMarginPercent { get; set; }
}
