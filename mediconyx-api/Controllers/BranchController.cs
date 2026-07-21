using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Middleware;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Common;

namespace mediconyx_api.Controllers;

/// <summary>
/// Branch (hospital location) management. Max Healthcare model: one tenant (brand),
/// many branches. All endpoints are tenant-scoped via the TenantId JWT claim.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BranchController> _logger;

    public BranchController(ApplicationDbContext context, ILogger<BranchController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int? TenantId => ClaimsHelper.GetTenantId(User);

    [HttpGet]
    [HasPermission(PermissionCatalog.TENANT_SETTINGS_VIEW)]
    public async Task<ActionResult<List<BranchDto>>> GetBranches([FromQuery] bool includeInactive = false)
    {
        if (TenantId == null) return Unauthorized();
        var q = _context.Branches.Where(b => b.TenantId == TenantId);
        if (!includeInactive) q = q.Where(b => b.IsActive);
        var branches = await q.OrderByDescending(b => b.IsMain).ThenBy(b => b.Name).ToListAsync();
        return Ok(branches.Select(MapToDto).ToList());
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCatalog.TENANT_SETTINGS_VIEW)]
    public async Task<ActionResult<BranchDto>> GetBranch(int id)
    {
        if (TenantId == null) return Unauthorized();
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id && b.TenantId == TenantId);
        if (branch == null) return NotFound();
        return Ok(MapToDto(branch));
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.TENANT_SETTINGS_EDIT)]
    public async Task<ActionResult<BranchDto>> CreateBranch(CreateBranchRequest req)
    {
        if (TenantId == null) return Unauthorized();

        var code = req.Code.Trim().ToUpperInvariant();
        if (await _context.Branches.AnyAsync(b => b.TenantId == TenantId && b.Code == code))
            return BadRequest(new { message = $"Branch code '{code}' already exists in this hospital." });

        // First branch of a tenant is always the main one; a new IsMain demotes the previous.
        var hasAny = await _context.Branches.AnyAsync(b => b.TenantId == TenantId);
        var isMain = req.IsMain || !hasAny;
        if (isMain && hasAny)
            await DemoteCurrentMainAsync();

        var branch = new Branch
        {
            TenantId = TenantId.Value,
            Name = req.Name.Trim(),
            Code = code,
            Address = req.Address,
            City = req.City,
            Phone = req.Phone,
            Email = req.Email,
            IsMain = isMain,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Branch {BranchId} ({Code}) created for tenant {TenantId}", branch.Id, branch.Code, TenantId);
        return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, MapToDto(branch));
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionCatalog.TENANT_SETTINGS_EDIT)]
    public async Task<ActionResult<BranchDto>> UpdateBranch(int id, UpdateBranchRequest req)
    {
        if (TenantId == null) return Unauthorized();
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id && b.TenantId == TenantId);
        if (branch == null) return NotFound();

        if (req.Name != null) branch.Name = req.Name.Trim();
        if (req.Address != null) branch.Address = req.Address;
        if (req.City != null) branch.City = req.City;
        if (req.Phone != null) branch.Phone = req.Phone;
        if (req.Email != null) branch.Email = req.Email;

        if (req.IsMain == true && !branch.IsMain)
        {
            await DemoteCurrentMainAsync();
            branch.IsMain = true;
        }
        if (req.IsActive == false && branch.IsMain)
            return BadRequest(new { message = "Cannot deactivate the main branch. Promote another branch to main first." });
        if (req.IsActive.HasValue) branch.IsActive = req.IsActive.Value;

        branch.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(MapToDto(branch));
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCatalog.TENANT_SETTINGS_EDIT)]
    public async Task<IActionResult> DeleteBranch(int id)
    {
        if (TenantId == null) return Unauthorized();
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id && b.TenantId == TenantId);
        if (branch == null) return NotFound();
        if (branch.IsMain)
            return BadRequest(new { message = "Cannot delete the main branch. Promote another branch to main first." });

        // Soft delete — branch may be referenced by wards/beds/admissions (Stage 2+).
        branch.IsActive = false;
        branch.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task DemoteCurrentMainAsync()
    {
        var currentMain = await _context.Branches
            .Where(b => b.TenantId == TenantId && b.IsMain)
            .ToListAsync();
        foreach (var b in currentMain) b.IsMain = false;
    }

    private static BranchDto MapToDto(Branch b) => new()
    {
        Id = b.Id,
        TenantId = b.TenantId,
        Name = b.Name,
        Code = b.Code,
        Address = b.Address,
        City = b.City,
        Phone = b.Phone,
        Email = b.Email,
        IsMain = b.IsMain,
        IsActive = b.IsActive,
        CreatedAt = b.CreatedAt
    };
}
