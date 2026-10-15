namespace mediconyx_api.Models.DTOs;

public class AdminKpiQueryParams
{
    /// <summary>"today", "week", "month", or custom ISO dates.</summary>
    public string? DateRange { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? BranchId { get; set; }
    /// <summary>"daily", "weekly", "monthly" for trend data.</summary>
    public string? Granularity { get; set; }
}
