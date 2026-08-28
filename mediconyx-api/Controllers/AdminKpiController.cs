using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Middleware;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Accounting;
using mediconyx_api.Services.Common;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/admin/kpis")]
[Authorize]
public class AdminKpiController : ControllerBase
{
    private readonly IAdminKpiService _kpiService;

    public AdminKpiController(IAdminKpiService kpiService)
    {
        _kpiService = kpiService;
    }

    [HttpGet("financial")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<AdminFinancialKpiDto>> GetFinancialKpis(
        [FromQuery] AdminKpiQueryParams query)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _kpiService.GetFinancialKpisAsync(tenantId.Value, query));
    }

    [HttpGet("clinical")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<AdminClinicalKpiDto>> GetClinicalKpis(
        [FromQuery] AdminKpiQueryParams query)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _kpiService.GetClinicalKpisAsync(tenantId.Value, query));
    }

    [HttpGet("departmental-revenue")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<List<DepartmentalRevenueDto>>> GetDepartmentalRevenue(
        [FromQuery] AdminKpiQueryParams query)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _kpiService.GetDepartmentalRevenueAsync(tenantId.Value, query));
    }

    [HttpGet("revenue-trend")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<List<RevenueTrendDto>>> GetRevenueTrend(
        [FromQuery] AdminKpiQueryParams query)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();
        return Ok(await _kpiService.GetRevenueTrendAsync(tenantId.Value, query));
    }
}
