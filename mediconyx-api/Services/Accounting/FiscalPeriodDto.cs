using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Accounting;

public class FiscalPeriodDto
{
    public int Id { get; set; }
    public int FiscalYearId { get; set; }
    public int Year { get; set; }
    public int PeriodNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
}
