using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Middleware;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Accounting;
using mediconyx_api.Services.Common;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/accounting")]
[Authorize]
public class AccountingController : ControllerBase
{
    private readonly ILedgerService _ledgerService;

    public AccountingController(ILedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    // ── Chart of Accounts ──────────────────────────────────────────────────

    [HttpGet("chart-of-accounts")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<List<AccountDto>>> GetChartOfAccounts()
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _ledgerService.GetChartOfAccountsAsync(tenantId.Value));
    }

    [HttpPost("accounts")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        try
        {
            var account = await _ledgerService.CreateAccountAsync(tenantId.Value, request);
            return CreatedAtAction(nameof(GetChartOfAccounts), account);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("accounts/{id}")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult<AccountDto>> UpdateAccount(int id, [FromBody] UpdateAccountRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        try
        {
            var account = await _ledgerService.UpdateAccountAsync(tenantId.Value, id, request);
            return Ok(account);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Journal Entries ────────────────────────────────────────────────────

    [HttpGet("journal-entries")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult> GetJournalEntries(
        [FromQuery] int? branchId,
        [FromQuery] int? fiscalPeriodId,
        [FromQuery] JournalEntrySourceType? sourceType,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        var (entries, totalCount) = await _ledgerService.GetJournalEntriesAsync(
            tenantId.Value, branchId, fiscalPeriodId, sourceType,
            dateFrom, dateTo, pageNumber, pageSize);

        return Ok(new
        {
            entries,
            totalCount,
            pageNumber = Math.Max(pageNumber, 1),
            pageSize = Math.Min(pageSize, 100),
            totalPages = (int)Math.Ceiling(totalCount / (double)Math.Min(pageSize, 100))
        });
    }

    [HttpGet("journal-entries/{id}")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<JournalEntryDto>> GetJournalEntry(int id)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        var entry = await _ledgerService.GetJournalEntryByIdAsync(tenantId.Value, id);
        return entry == null ? NotFound() : Ok(entry);
    }

    [HttpPost("journal-entries")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult<JournalEntryDto>> CreateJournalEntry(
        [FromBody] CreateJournalEntryRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        var userId = ClaimsHelper.GetUserId(User);
        if (tenantId == null || userId == null) return Unauthorized();

        try
        {
            var entry = await _ledgerService.PostJournalEntryAsync(tenantId.Value, userId.Value, request);
            return CreatedAtAction(nameof(GetJournalEntry), new { id = entry.Id }, entry);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("journal-entries/{id}/reverse")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult<JournalEntryDto>> ReverseJournalEntry(
        int id, [FromBody] ReverseJournalEntryRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        var userId = ClaimsHelper.GetUserId(User);
        if (tenantId == null || userId == null) return Unauthorized();

        try
        {
            var reversal = await _ledgerService.ReverseJournalEntryAsync(
                tenantId.Value, id, userId.Value, request.Reason);
            return Ok(reversal);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ── Financial Reports ──────────────────────────────────────────────────

    [HttpGet("trial-balance")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<TrialBalanceDto>> GetTrialBalance([FromQuery] int fiscalPeriodId)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        try
        {
            return Ok(await _ledgerService.GetTrialBalanceAsync(tenantId.Value, fiscalPeriodId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("balance-sheet")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<BalanceSheetDto>> GetBalanceSheet([FromQuery] DateTime? asOfDate)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _ledgerService.GetBalanceSheetAsync(tenantId.Value, asOfDate ?? DateTime.UtcNow));
    }

    [HttpGet("income-statement")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<IncomeStatementDto>> GetIncomeStatement(
        [FromQuery] DateTime? periodStart, [FromQuery] DateTime? periodEnd)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        var start = periodStart ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = periodEnd ?? DateTime.UtcNow;

        return Ok(await _ledgerService.GetIncomeStatementAsync(tenantId.Value, start, end));
    }

    // ── Fiscal Periods ─────────────────────────────────────────────────────

    [HttpGet("fiscal-periods")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<List<FiscalPeriodDto>>> GetFiscalPeriods()
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _ledgerService.GetFiscalPeriodsAsync(tenantId.Value));
    }

    [HttpPost("fiscal-periods/{id}/close")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult> CloseFiscalPeriod(int id)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        var userId = ClaimsHelper.GetUserId(User);
        if (tenantId == null || userId == null) return Unauthorized();

        try
        {
            await _ledgerService.CloseFiscalPeriodAsync(tenantId.Value, id, userId.Value);
            return Ok(new { message = "Fiscal period closed successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
