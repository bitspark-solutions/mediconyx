using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using System.Security.Claims;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(ApplicationDbContext context, ILogger<InvoiceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<InvoiceListResponse>> GetInvoices([FromQuery] InvoiceQueryParams queryParams)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Invoices
            .Where(i => i.TenantId == tenantId.Value)
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
            "createdat" or _ => queryParams.SortOrder == "asc"
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

        return Ok(new InvoiceListResponse
        {
            Invoices = await MapToDtoList(invoices),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId.Value);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        return Ok(await MapToDto(invoice));
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<InvoiceListResponse>> GetInvoicesByPatient(
        int patientId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Invoices
            .Where(i => i.TenantId == tenantId.Value && i.PatientId == patientId)
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

        return Ok(new InvoiceListResponse
        {
            Invoices = await MapToDtoList(invoices),
            TotalCount = totalCount,
            PageNumber = pageNumberClamped,
            PageSize = pageSizeClamped,
            TotalPages = totalPages
        });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<RevenueStatsDto>> GetRevenueStats()
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var invoices = await _context.Invoices
            .Where(i => i.TenantId == tenantId.Value)
            .ToListAsync();

        var todayRevenue = invoices
            .Where(i => i.PaidAt.HasValue && i.PaidAt.Value.Date == today)
            .Sum(i => i.PaidAmount);

        var monthRevenue = invoices
            .Where(i => i.PaidAt.HasValue && i.PaidAt.Value >= monthStart)
            .Sum(i => i.PaidAmount);

        var outstandingAmount = invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Refunded)
            .Sum(i => i.TotalAmount - i.PaidAmount);

        var stats = new RevenueStatsDto
        {
            TodayRevenue = todayRevenue,
            MonthRevenue = monthRevenue,
            OutstandingAmount = outstandingAmount,
            TotalInvoices = invoices.Count,
            PaidCount = invoices.Count(i => i.Status == InvoiceStatus.Paid),
            OverdueCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue)
        };

        return Ok(stats);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice(CreateInvoiceRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null) return Unauthorized();

        if (request.PatientId <= 0)
            return BadRequest(new { message = "Patient ID is required." });
        if (string.IsNullOrWhiteSpace(request.LineItems))
            return BadRequest(new { message = "Line items are required." });

        var invoiceNumber = await GenerateInvoiceNumber(tenantId.Value);

        var invoice = new Invoice
        {
            TenantId = tenantId.Value,
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
            CreatedByUserId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice created: {InvoiceNumber} for Patient {PatientId} in Tenant {TenantId}",
            invoice.InvoiceNumber, invoice.PatientId, tenantId.Value);

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, await MapToDto(invoice));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<InvoiceDto>> UpdateInvoice(int id, UpdateInvoiceRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId.Value);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        if (invoice.Status != InvoiceStatus.Draft)
            return BadRequest(new { message = "Only draft invoices can be updated." });

        if (request.LineItems != null)
            invoice.LineItems = request.LineItems.Trim();
        if (request.SubTotal.HasValue)
            invoice.SubTotal = request.SubTotal.Value;
        if (request.TaxRate.HasValue)
            invoice.TaxRate = request.TaxRate.Value;
        if (request.TaxAmount.HasValue)
            invoice.TaxAmount = request.TaxAmount.Value;
        if (request.DiscountAmount.HasValue)
            invoice.DiscountAmount = request.DiscountAmount.Value;
        if (request.TotalAmount.HasValue)
            invoice.TotalAmount = request.TotalAmount.Value;
        if (request.DueDate.HasValue)
            invoice.DueDate = request.DueDate.Value;

        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice updated: {InvoiceNumber} by User {UserId}", invoice.InvoiceNumber, GetCurrentUserId());

        return Ok(await MapToDto(invoice));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<InvoiceDto>> UpdateInvoiceStatus(int id, [FromBody] InvoiceStatus status)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId.Value);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        invoice.Status = status;
        invoice.UpdatedAt = DateTime.UtcNow;

        if (status == InvoiceStatus.Issued && invoice.DueDate == null)
            invoice.DueDate = DateTime.UtcNow.AddDays(30);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice status updated: {InvoiceNumber} to {Status}", invoice.InvoiceNumber, status);

        return Ok(await MapToDto(invoice));
    }

    [HttpPost("{id}/payments")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<InvoiceDto>> RecordPayment(int id, RecordPaymentRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId.Value);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        if (invoice.Status == InvoiceStatus.Cancelled || invoice.Status == InvoiceStatus.Refunded)
            return BadRequest(new { message = "Cannot record payment for cancelled or refunded invoice." });

        if (request.Amount <= 0)
            return BadRequest(new { message = "Payment amount must be greater than zero." });

        var balanceDue = invoice.TotalAmount - invoice.PaidAmount;
        if (request.Amount > balanceDue)
            return BadRequest(new { message = $"Payment amount exceeds balance due: {balanceDue:F2}" });

        invoice.PaidAmount += request.Amount;
        invoice.PaymentMethod = request.PaymentMethod;
        invoice.PaymentReference = request.PaymentReference?.Trim();
        invoice.UpdatedAt = DateTime.UtcNow;

        if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment recorded for invoice {InvoiceNumber}: {Amount} via {Method}",
            invoice.InvoiceNumber, request.Amount, request.PaymentMethod);

        return Ok(await MapToDto(invoice));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId.Value);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        if (invoice.Status != InvoiceStatus.Draft)
            return BadRequest(new { message = "Only draft invoices can be deleted." });

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice deleted: {InvoiceNumber} by Admin {AdminId}", invoice.InvoiceNumber, GetCurrentUserId());

        return NoContent();
    }

    private async Task<string> GenerateInvoiceNumber(int tenantId)
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";
        var maxNumber = await _context.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (maxNumber != null && int.TryParse(maxNumber[prefix.Length..], out var current))
        {
            nextSeq = current + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetCurrentTenantId()
    {
        var claim = User.FindFirst("TenantId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private async Task<InvoiceDto> MapToDto(Invoice i)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == i.PatientId);

        return new InvoiceDto
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

    private async Task<List<InvoiceDto>> MapToDtoList(List<Invoice> invoices)
    {
        var patientIds = invoices.Select(i => i.PatientId).Distinct().ToList();

        var patients = await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        return invoices.Select(i =>
        {
            var patient = patients.TryGetValue(i.PatientId, out var p) ? p : null;
            return new InvoiceDto
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
        }).ToList();
    }
}