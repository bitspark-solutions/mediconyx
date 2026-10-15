namespace mediconyx_api.Models.DTOs;

public class DepartmentalRevenueDto
{
    public int? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public decimal MarginPercent { get; set; }
}
