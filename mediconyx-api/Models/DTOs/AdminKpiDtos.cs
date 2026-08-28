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

public class DepartmentalRevenueDto
{
    public int? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public decimal MarginPercent { get; set; }
}

public class AdminClinicalKpiDto
{
    public int ActiveAdmissions { get; set; }
    public int TotalBeds { get; set; }
    public decimal BedOccupancyPercent { get; set; }
    public int DischargedPatients { get; set; }
    public decimal AverageLengthOfStay { get; set; }
    public int DeceasedPatients { get; set; }
    public decimal MortalityRatePercent { get; set; }
    public int NewBirths { get; set; }
}

public class RevenueTrendDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
}

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
