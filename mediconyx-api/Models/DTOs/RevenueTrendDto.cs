namespace mediconyx_api.Models.DTOs;

public class RevenueTrendDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
}
