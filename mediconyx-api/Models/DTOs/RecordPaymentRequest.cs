namespace mediconyx_api.Models.DTOs;

public class RecordPaymentRequest
{
    public decimal Amount { get; set; }
    public InvoicePaymentMethod PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
}
