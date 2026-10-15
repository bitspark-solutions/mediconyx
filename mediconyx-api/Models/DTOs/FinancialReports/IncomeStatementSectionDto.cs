namespace mediconyx_api.Models.DTOs;

public class IncomeStatementSectionDto
{
    public string SectionName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<IncomeStatementLineDto> Lines { get; set; } = new();
}
