namespace mediconyx_api.Models.DTOs;

public class InvoiceQueryParams
{
    public int? PatientId { get; set; }
    public int? BranchId { get; set; }
    public InvoiceStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortOrder { get; set; } = "desc";
}
