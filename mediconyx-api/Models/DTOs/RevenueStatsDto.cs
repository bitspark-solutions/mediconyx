namespace mediconyx_api.Models.DTOs;

public class RevenueStatsDto
{
    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int TotalInvoices { get; set; }
    public int PaidCount { get; set; }
    public int OverdueCount { get; set; }
}
