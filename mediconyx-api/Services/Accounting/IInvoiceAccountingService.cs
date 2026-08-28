using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Accounting;

public interface IInvoiceAccountingService
{
    /// <summary>Generate journal entry when an invoice is issued (DR: AR, CR: Revenue per line).</summary>
    Task OnInvoiceIssuedAsync(int tenantId, int invoiceId, int userId);

    /// <summary>Generate journal entry when a payment is recorded (DR: Cash/Bank, CR: AR).</summary>
    Task OnPaymentReceivedAsync(int tenantId, int invoiceId, decimal amount,
        InvoicePaymentMethod paymentMethod, int userId);

    /// <summary>Generate journal entry for a refund (DR: Refunds Payable, CR: Cash).</summary>
    Task OnRefundIssuedAsync(int tenantId, int invoiceId, decimal amount, int userId);
}
