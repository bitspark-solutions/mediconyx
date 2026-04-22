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
public class DepartmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(ApplicationDbContext context, ILogger<DepartmentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List departments for tenant (paginated)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DepartmentListResponse>> GetDepartments([FromQuery] DepartmentQueryParams queryParams)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Departments.Where(d => d.TenantId == tenantId.Value).AsQueryable();

        if (queryParams.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == queryParams.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var search = queryParams.SearchTerm.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(search) ||
                d.Code.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var pageSize = Math.Min(queryParams.PageSize, 100);
        var pageNumber = Math.Max(queryParams.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var departments = await query
            .OrderBy(d => d.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new DepartmentListResponse
        {
            Departments = departments.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return Ok(response);
    }

    /// <summary>
    /// Get single department by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var dept = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value);

        if (dept == null)
        {
            return NotFound(new { message = "Department not found." });
        }

        return Ok(MapToDto(dept));
    }

    /// <summary>
    /// Create department (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(CreateDepartmentRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Department name is required." });
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Department code is required." });

        // Check for duplicate code within tenant
        if (await _context.Departments.AnyAsync(d =>
            d.TenantId == tenantId.Value && d.Code.ToLower() == request.Code.ToLower()))
        {
            return BadRequest(new { message = "A department with this code already exists." });
        }

        var dept = new Department
        {
            TenantId = tenantId.Value,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpper(),
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department created: {Code} in Tenant {TenantId}", dept.Code, tenantId.Value);

        return CreatedAtAction(nameof(GetDepartment), new { id = dept.Id }, MapToDto(dept));
    }

    /// <summary>
    /// Update department (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(int id, UpdateDepartmentRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var dept = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value);

        if (dept == null)
        {
            return NotFound(new { message = "Department not found." });
        }

        if (!string.IsNullOrWhiteSpace(request.Name)) dept.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var newCode = request.Code.Trim().ToUpper();
            if (newCode != dept.Code)
            {
                if (await _context.Departments.AnyAsync(d =>
                    d.TenantId == tenantId.Value && d.Code == newCode && d.Id != id))
                {
                    return BadRequest(new { message = "A department with this code already exists." });
                }
                dept.Code = newCode;
            }
        }
        if (request.Description != null) dept.Description = request.Description.Trim();
        if (request.IsActive.HasValue) dept.IsActive = request.IsActive.Value;

        dept.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department updated: {DeptId} by User {UserId}", id, GetCurrentUserId());

        return Ok(MapToDto(dept));
    }

    /// <summary>
    /// Soft-delete department (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var dept = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value);

        if (dept == null)
        {
            return NotFound(new { message = "Department not found." });
        }

        dept.IsActive = false;
        dept.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department deactivated: {DeptId} by Admin {AdminId}", id, GetCurrentUserId());

        return NoContent();
    }

    // Helper methods
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

    private static DepartmentDto MapToDto(Department d)
    {
        return new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            Description = d.Description,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt
        };
    }
}
