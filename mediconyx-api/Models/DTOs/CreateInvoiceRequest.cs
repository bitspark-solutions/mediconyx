namespace mediconyx_api.Models.DTOs;

public class CreateInvoiceRequest
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public string LineItems { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? DueDate { get; set; }
}
