namespace mediconyx_api.Models;

public class Invoice
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    // Line items as plain text (one per line)
    // Format: "Description | Qty | UnitPrice | Total"
    public string LineItems { get; set; } = string.Empty;

    // Totals
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    // Status & Payment
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public InvoicePaymentMethod? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }

    // Audit
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Paid = 2,
    PartiallyPaid = 3,
    Overdue = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum InvoicePaymentMethod
{
    Cash = 0,
    Card = 1,
    BankTransfer = 2,
    bKash = 3,
    Nagad = 4,
    Rocket = 5,
    Insurance = 6,
    Other = 7
}
