namespace mediconyx_api.Models.DTOs;

public class BalanceSheetSectionDto
{
    public string SectionName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<BalanceSheetLineDto> Lines { get; set; } = new();
}
