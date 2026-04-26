using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Invoices;

public interface IInvoiceService
{
    Task<InvoiceListResponse> GetInvoicesAsync(int tenantId, InvoiceQueryParams queryParams);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int tenantId, int invoiceId);
    Task<InvoiceListResponse> GetInvoicesByPatientAsync(int tenantId, int patientId, int pageNumber, int pageSize);
    Task<RevenueStatsDto> GetRevenueStatsAsync(int tenantId);
    Task<InvoiceDto> CreateInvoiceAsync(int tenantId, int createdByUserId, CreateInvoiceRequest request);
    Task<InvoiceDto> UpdateInvoiceAsync(int tenantId, int invoiceId, UpdateInvoiceRequest request);
    Task<InvoiceDto> UpdateInvoiceStatusAsync(int tenantId, int invoiceId, InvoiceStatus status);
    Task<InvoiceDto> RecordPaymentAsync(int tenantId, int invoiceId, RecordPaymentRequest request);
    Task DeleteInvoiceAsync(int tenantId, int invoiceId);
}
