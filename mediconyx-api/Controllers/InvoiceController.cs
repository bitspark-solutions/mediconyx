using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Middleware;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Common;
using mediconyx_api.Services.Invoices;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<InvoiceListResponse>> GetInvoices([FromQuery] InvoiceQueryParams queryParams)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _invoiceService.GetInvoicesAsync(tenantId.Value, queryParams));
    }

    [HttpGet("{id}")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        var invoice = await _invoiceService.GetInvoiceByIdAsync(tenantId.Value, id);
        return invoice == null ? NotFound(new { message = "Invoice not found." }) : Ok(invoice);
    }

    [HttpGet("patient/{patientId}")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<InvoiceListResponse>> GetInvoicesByPatient(
        int patientId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _invoiceService.GetInvoicesByPatientAsync(tenantId.Value, patientId, pageNumber, pageSize));
    }

    [HttpGet("stats")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<RevenueStatsDto>> GetRevenueStats()
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _invoiceService.GetRevenueStatsAsync(tenantId.Value));
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice(CreateInvoiceRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        var userId = ClaimsHelper.GetUserId(User);
        if (tenantId == null || userId == null) return Unauthorized();

        if (request.PatientId <= 0) return BadRequest(new { message = "Patient ID is required." });
        if (string.IsNullOrWhiteSpace(request.LineItems)) return BadRequest(new { message = "Line items are required." });

        var invoice = await _invoiceService.CreateInvoiceAsync(tenantId.Value, userId.Value, request);
        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    [HttpPut("{id}")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult<InvoiceDto>> UpdateInvoice(int id, UpdateInvoiceRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        try
        {
            return Ok(await _invoiceService.UpdateInvoiceAsync(tenantId.Value, id, request));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "Invoice not found." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/status")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult<InvoiceDto>> UpdateInvoiceStatus(int id, [FromBody] InvoiceStatus status)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        try
        {
            return Ok(await _invoiceService.UpdateInvoiceStatusAsync(tenantId.Value, id, status));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "Invoice not found." }); }
    }

    [HttpPost("{id}/payments")]
    [HasPermission(PermissionCatalog.PAYMENT_RECORD)]
    public async Task<ActionResult<InvoiceDto>> RecordPayment(int id, RecordPaymentRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        try
        {
            return Ok(await _invoiceService.RecordPaymentAsync(tenantId.Value, id, request));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "Invoice not found." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        try
        {
            await _invoiceService.DeleteInvoiceAsync(tenantId.Value, id);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "Invoice not found." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}