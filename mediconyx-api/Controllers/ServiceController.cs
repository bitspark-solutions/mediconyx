using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceController> _logger;

    public ServiceController(ApplicationDbContext context, ILogger<ServiceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List all services for the tenant
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist")]
    public async Task<ActionResult<List<HospitalServiceDto>>> GetServices([FromQuery] bool includeInactive = false)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.HospitalServices
            .Where(s => s.TenantId == tenantId);

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        var services = await query
            .Include(s => s.Department)
            .OrderBy(s => s.ServiceType)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return Ok(services.Select(s => new HospitalServiceDto
        {
            Id = s.Id,
            Name = s.Name,
            ServiceType = s.ServiceType,
            Description = s.Description,
            DurationMinutes = s.DurationMinutes,
            BasePrice = s.BasePrice,
            DepartmentId = s.DepartmentId,
            DepartmentName = s.Department?.Name,
            IsActive = s.IsActive
        }));
    }

    /// <summary>
    /// Get a single service by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist")]
    public async Task<ActionResult<HospitalServiceDto>> GetService(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.HospitalServices
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (service == null) return NotFound();

        return Ok(new HospitalServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            ServiceType = service.ServiceType,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            BasePrice = service.BasePrice,
            DepartmentId = service.DepartmentId,
            DepartmentName = service.Department?.Name,
            IsActive = service.IsActive
        });
    }

    /// <summary>
    /// Create a new service
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<HospitalServiceDto>> CreateService([FromBody] CreateHospitalServiceRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        if (!ServiceTypes.All.Contains(request.ServiceType))
            return BadRequest(new { message = $"Invalid service type. Allowed: {string.Join(", ", ServiceTypes.All)}" });

        var service = new HospitalService
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            ServiceType = request.ServiceType,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            BasePrice = request.BasePrice,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.HospitalServices.Add(service);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Service {ServiceId} created for tenant {TenantId}", service.Id, tenantId);

        return CreatedAtAction(nameof(GetService), new { id = service.Id }, new HospitalServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            ServiceType = service.ServiceType,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            BasePrice = service.BasePrice,
            DepartmentId = service.DepartmentId,
            IsActive = service.IsActive
        });
    }

    /// <summary>
    /// Update a service
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<HospitalServiceDto>> UpdateService(int id, [FromBody] UpdateHospitalServiceRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.HospitalServices
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (service == null) return NotFound();

        if (request.Name != null) service.Name = request.Name;
        if (request.ServiceType != null)
        {
            if (!ServiceTypes.All.Contains(request.ServiceType))
                return BadRequest(new { message = $"Invalid service type. Allowed: {string.Join(", ", ServiceTypes.All)}" });
            service.ServiceType = request.ServiceType;
        }
        if (request.Description != null) service.Description = request.Description;
        if (request.DurationMinutes.HasValue) service.DurationMinutes = request.DurationMinutes.Value;
        if (request.BasePrice.HasValue) service.BasePrice = request.BasePrice;
        if (request.DepartmentId.HasValue) service.DepartmentId = request.DepartmentId;
        if (request.IsActive.HasValue) service.IsActive = request.IsActive.Value;

        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Service {ServiceId} updated", id);

        return Ok(new HospitalServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            ServiceType = service.ServiceType,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            BasePrice = service.BasePrice,
            DepartmentId = service.DepartmentId,
            IsActive = service.IsActive
        });
    }

    /// <summary>
    /// Delete (soft) a service
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteService(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.HospitalServices
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (service == null) return NotFound();

        service.IsActive = false;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Service {ServiceId} deactivated", id);

        return NoContent();
    }

    private int? GetCurrentTenantId()
    {
        var claim = User.FindFirst("TenantId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}