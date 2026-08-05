namespace mediconyx_api.Models.DTOs;

public class CreateInvoiceRequest
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    /// <summary>Branch this invoice is billed at. Defaults to the tenant's main branch when omitted.</summary>
    public int? BranchId { get; set; }
    public string LineItems { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? DueDate { get; set; }
}
