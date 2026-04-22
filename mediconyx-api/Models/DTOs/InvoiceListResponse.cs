namespace mediconyx_api.Models.DTOs;

public class InvoiceListResponse
{
    public List<InvoiceDto> Invoices { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
