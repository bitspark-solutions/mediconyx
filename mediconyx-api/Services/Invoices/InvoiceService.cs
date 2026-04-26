using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Invoices;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(ApplicationDbContext context, ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InvoiceListResponse> GetInvoicesAsync(int tenantId, InvoiceQueryParams queryParams)
    {
        var query = _context.Invoices
            .Where(i => i.TenantId == tenantId)
            .AsQueryable();

        if (queryParams.PatientId.HasValue)
            query = query.Where(i => i.PatientId == queryParams.PatientId.Value);

        if (queryParams.Status.HasValue)
            query = query.Where(i => i.Status == queryParams.Status.Value);

        if (queryParams.DateFrom.HasValue)
            query = query.Where(i => i.CreatedAt >= queryParams.DateFrom.Value);

        if (queryParams.DateTo.HasValue)
            query = query.Where(i => i.CreatedAt <= queryParams.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var search = queryParams.SearchTerm.ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(search) ||
                (i.PaymentReference != null && i.PaymentReference.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        query = queryParams.SortBy?.ToLower() switch
        {
            "invoicenumber" => queryParams.SortOrder == "asc"
                ? query.OrderBy(i => i.InvoiceNumber)
                : query.OrderByDescending(i => i.InvoiceNumber),
            "totalamount" => queryParams.SortOrder == "asc"
                ? query.OrderBy(i => i.TotalAmount)
                : query.OrderByDescending(i => i.TotalAmount),
            "duedate" => queryParams.SortOrder == "asc"
                ? query.OrderBy(i => i.DueDate)
                : query.OrderByDescending(i => i.DueDate),
            _ => queryParams.SortOrder == "asc"
                ? query.OrderBy(i => i.CreatedAt)
                : query.OrderByDescending(i => i.CreatedAt)
        };

        var pageSize = Math.Min(queryParams.PageSize, 100);
        var pageNumber = Math.Max(queryParams.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var invoices = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new InvoiceListResponse
        {
            Invoices = await MapToDtoListAsync(invoices),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int tenantId, int invoiceId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);

        return invoice is null ? null : await MapToDtoAsync(invoice);
    }

    public async Task<InvoiceListResponse> GetInvoicesByPatientAsync(int tenantId, int patientId, int pageNumber, int pageSize)
    {
        var query = _context.Invoices
            .Where(i => i.TenantId == tenantId && i.PatientId == patientId)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var pageSizeClamped = Math.Min(pageSize, 100);
        var pageNumberClamped = Math.Max(pageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSizeClamped);

        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumberClamped - 1) * pageSizeClamped)
            .Take(pageSizeClamped)
            .ToListAsync();

        return new InvoiceListResponse
        {
            Invoices = await MapToDtoListAsync(invoices),
            TotalCount = totalCount,
            PageNumber = pageNumberClamped,
            PageSize = pageSizeClamped,
            TotalPages = totalPages
        };
    }

    public async Task<RevenueStatsDto> GetRevenueStatsAsync(int tenantId)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var invoices = await _context.Invoices
            .Where(i => i.TenantId == tenantId)
            .ToListAsync();

        return new RevenueStatsDto
        {
            TodayRevenue = invoices
                .Where(i => i.PaidAt.HasValue && i.PaidAt.Value.Date == today)
                .Sum(i => i.PaidAmount),
            MonthRevenue = invoices
                .Where(i => i.PaidAt.HasValue && i.PaidAt.Value >= monthStart)
                .Sum(i => i.PaidAmount),
            OutstandingAmount = invoices
                .Where(i => i.Status != InvoiceStatus.Paid
                         && i.Status != InvoiceStatus.Cancelled
                         && i.Status != InvoiceStatus.Refunded)
                .Sum(i => i.TotalAmount - i.PaidAmount),
            TotalInvoices = invoices.Count,
            PaidCount = invoices.Count(i => i.Status == InvoiceStatus.Paid),
            OverdueCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue)
        };
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(int tenantId, int createdByUserId, CreateInvoiceRequest request)
    {
        var invoiceNumber = await GenerateInvoiceNumberAsync(tenantId);

        var invoice = new Invoice
        {
            TenantId = tenantId,
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId > 0 ? request.AppointmentId : null,
            InvoiceNumber = invoiceNumber,
            LineItems = request.LineItems.Trim(),
            SubTotal = request.SubTotal,
            TaxRate = request.TaxRate,
            TaxAmount = request.TaxAmount,
            DiscountAmount = request.DiscountAmount,
            TotalAmount = request.TotalAmount,
            PaidAmount = 0,
            Status = InvoiceStatus.Draft,
            DueDate = request.DueDate,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice created: {InvoiceNumber} for Patient {PatientId} in Tenant {TenantId}",
            invoice.InvoiceNumber, invoice.PatientId, tenantId);

        return await MapToDtoAsync(invoice);
    }

    public async Task<InvoiceDto> UpdateInvoiceAsync(int tenantId, int invoiceId, UpdateInvoiceRequest request)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be updated.");

        if (request.LineItems != null) invoice.LineItems = request.LineItems.Trim();
        if (request.SubTotal.HasValue) invoice.SubTotal = request.SubTotal.Value;
        if (request.TaxRate.HasValue) invoice.TaxRate = request.TaxRate.Value;
        if (request.TaxAmount.HasValue) invoice.TaxAmount = request.TaxAmount.Value;
        if (request.DiscountAmount.HasValue) invoice.DiscountAmount = request.DiscountAmount.Value;
        if (request.TotalAmount.HasValue) invoice.TotalAmount = request.TotalAmount.Value;
        if (request.DueDate.HasValue) invoice.DueDate = request.DueDate.Value;

        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice updated: {InvoiceNumber} in Tenant {TenantId}", invoice.InvoiceNumber, tenantId);

        return await MapToDtoAsync(invoice);
    }

    public async Task<InvoiceDto> UpdateInvoiceStatusAsync(int tenantId, int invoiceId, InvoiceStatus status)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        invoice.Status = status;
        invoice.UpdatedAt = DateTime.UtcNow;

        if (status == InvoiceStatus.Issued && invoice.DueDate == null)
            invoice.DueDate = DateTime.UtcNow.AddDays(30);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceNumber} status updated to {Status}", invoice.InvoiceNumber, status);

        return await MapToDtoAsync(invoice);
    }

    public async Task<InvoiceDto> RecordPaymentAsync(int tenantId, int invoiceId, RecordPaymentRequest request)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        if (invoice.Status == InvoiceStatus.Cancelled || invoice.Status == InvoiceStatus.Refunded)
            throw new InvalidOperationException("Cannot record payment for a cancelled or refunded invoice.");

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.");

        var balanceDue = invoice.TotalAmount - invoice.PaidAmount;
        if (request.Amount > balanceDue)
            throw new ArgumentException($"Payment amount exceeds balance due: {balanceDue:F2}");

        invoice.PaidAmount += request.Amount;
        invoice.PaymentMethod = request.PaymentMethod;
        invoice.PaymentReference = request.PaymentReference?.Trim();
        invoice.UpdatedAt = DateTime.UtcNow;

        invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;

        if (invoice.Status == InvoiceStatus.Paid)
            invoice.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment recorded for invoice {InvoiceNumber}: {Amount} via {Method}",
            invoice.InvoiceNumber, request.Amount, request.PaymentMethod);

        return await MapToDtoAsync(invoice);
    }

    public async Task DeleteInvoiceAsync(int tenantId, int invoiceId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be deleted.");

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceNumber} deleted in Tenant {TenantId}", invoice.InvoiceNumber, tenantId);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<string> GenerateInvoiceNumberAsync(int tenantId)
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";

        var maxNumber = await _context.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (maxNumber != null && int.TryParse(maxNumber[prefix.Length..], out var current))
            nextSeq = current + 1;

        return $"{prefix}{nextSeq:D4}";
    }

    private async Task<InvoiceDto> MapToDtoAsync(Invoice i)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == i.PatientId);
        return BuildDto(i, patient);
    }

    private async Task<List<InvoiceDto>> MapToDtoListAsync(List<Invoice> invoices)
    {
        var patientIds = invoices.Select(i => i.PatientId).Distinct().ToList();
        var patients = await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        return invoices.Select(i => BuildDto(i, patients.GetValueOrDefault(i.PatientId))).ToList();
    }

    private static InvoiceDto BuildDto(Invoice i, Patient? patient) => new()
    {
        Id = i.Id,
        InvoiceNumber = i.InvoiceNumber,
        PatientId = i.PatientId,
        PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "",
        PatientNumber = patient?.PatientNumber ?? "",
        AppointmentId = i.AppointmentId,
        LineItems = i.LineItems,
        SubTotal = i.SubTotal,
        TaxRate = i.TaxRate,
        TaxAmount = i.TaxAmount,
        DiscountAmount = i.DiscountAmount,
        TotalAmount = i.TotalAmount,
        PaidAmount = i.PaidAmount,
        BalanceDue = i.TotalAmount - i.PaidAmount,
        Status = i.Status.ToString(),
        DueDate = i.DueDate,
        PaidAt = i.PaidAt,
        PaymentMethod = i.PaymentMethod?.ToString(),
        PaymentReference = i.PaymentReference,
        CreatedAt = i.CreatedAt
    };
}
