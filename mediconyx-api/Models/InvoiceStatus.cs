namespace mediconyx_api.Models;

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
