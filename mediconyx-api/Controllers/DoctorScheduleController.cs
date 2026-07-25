using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Middleware;
using mediconyx_api.Models.DTOs;
using System.Security.Claims;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/doctor-schedule")]
[Authorize]
public class DoctorScheduleController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DoctorScheduleController> _logger;

    public DoctorScheduleController(ApplicationDbContext context, ILogger<DoctorScheduleController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<DoctorScheduleDto>>> GetSchedules([FromQuery] int? doctorId, [FromQuery] DayOfWeek? dayOfWeek)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.DoctorSchedules
            .Where(s => s.TenantId == tenantId.Value && s.IsActive)
            .AsQueryable();

        if (doctorId.HasValue)
            query = query.Where(s => s.DoctorId == doctorId.Value);

        if (dayOfWeek.HasValue)
            query = query.Where(s => s.DayOfWeek == dayOfWeek.Value);

        var schedules = await query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToListAsync();

        return Ok(await MapToDtoList(schedules));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DoctorScheduleDto>> GetSchedule(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId.Value);

        if (schedule == null)
            return NotFound(new { message = "Doctor schedule not found." });

        return Ok(await MapToDto(schedule));
    }

    [HttpGet("doctor/{doctorId}")]
    public async Task<ActionResult<List<DoctorScheduleDto>>> GetSchedulesByDoctor(int doctorId)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var schedules = await _context.DoctorSchedules
            .Where(s => s.TenantId == tenantId.Value && s.DoctorId == doctorId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return Ok(await MapToDtoList(schedules));
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.DOCTOR_PROFILE_EDIT)]
    public async Task<ActionResult<DoctorScheduleDto>> CreateSchedule(CreateDoctorScheduleRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.DoctorId <= 0)
            return BadRequest(new { message = "Doctor ID is required." });
        if (request.DepartmentId <= 0)
            return BadRequest(new { message = "Department ID is required." });
        if (string.IsNullOrWhiteSpace(request.StartTime))
            return BadRequest(new { message = "Start time is required." });
        if (string.IsNullOrWhiteSpace(request.EndTime))
            return BadRequest(new { message = "End time is required." });

        if (!TimeSpan.TryParse(request.StartTime, out var startTime))
            return BadRequest(new { message = "Invalid start time format." });
        if (!TimeSpan.TryParse(request.EndTime, out var endTime))
            return BadRequest(new { message = "Invalid end time format." });

        if (endTime <= startTime)
            return BadRequest(new { message = "End time must be after start time." });

        var exists = await _context.DoctorSchedules
            .AnyAsync(s => s.TenantId == tenantId.Value
                && s.DoctorId == request.DoctorId
                && s.DayOfWeek == request.DayOfWeek
                && s.StartTime == startTime
                && s.EndTime == endTime);

        if (exists)
            return BadRequest(new { message = "A schedule with the same doctor, day, and time already exists." });

        var schedule = new DoctorSchedule
        {
            TenantId = tenantId.Value,
            DoctorId = request.DoctorId,
            DepartmentId = request.DepartmentId,
            DayOfWeek = request.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = request.SlotDurationMinutes > 0 ? request.SlotDurationMinutes : 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.DoctorSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Doctor schedule created: {ScheduleId} for Doctor {DoctorId} in Tenant {TenantId}",
            schedule.Id, schedule.DoctorId, tenantId.Value);

        return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, await MapToDto(schedule));
    }

    [HttpPut("{id}")]
    [HasPermission(PermissionCatalog.DOCTOR_PROFILE_EDIT)]
    public async Task<ActionResult<DoctorScheduleDto>> UpdateSchedule(int id, UpdateDoctorScheduleRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId.Value);

        if (schedule == null)
            return NotFound(new { message = "Doctor schedule not found." });

        if (!string.IsNullOrWhiteSpace(request.StartTime) && TimeSpan.TryParse(request.StartTime, out var st))
            schedule.StartTime = st;
        if (!string.IsNullOrWhiteSpace(request.EndTime) && TimeSpan.TryParse(request.EndTime, out var et))
            schedule.EndTime = et;
        if (request.SlotDurationMinutes.HasValue && request.SlotDurationMinutes.Value > 0)
            schedule.SlotDurationMinutes = request.SlotDurationMinutes.Value;
        if (request.IsActive.HasValue)
            schedule.IsActive = request.IsActive.Value;

        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Doctor schedule updated: {ScheduleId} by User {UserId}", id, GetCurrentUserId());

        return Ok(await MapToDto(schedule));
    }

    [HttpDelete("{id}")]
    [HasPermission(PermissionCatalog.DOCTOR_PROFILE_EDIT)]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId.Value);

        if (schedule == null)
            return NotFound(new { message = "Doctor schedule not found." });

        schedule.IsActive = false;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Doctor schedule deactivated: {ScheduleId} by Admin {AdminId}", id, GetCurrentUserId());

        return NoContent();
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

    private async Task<DoctorScheduleDto> MapToDto(DoctorSchedule s)
    {
        var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == s.DoctorId);
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == s.DepartmentId);

        return new DoctorScheduleDto
        {
            Id = s.Id,
            DoctorId = s.DoctorId,
            DoctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "",
            DepartmentId = s.DepartmentId,
            DepartmentName = department?.Name ?? "",
            DayOfWeek = s.DayOfWeek.ToString(),
            StartTime = s.StartTime.ToString(@"hh\:mm"),
            EndTime = s.EndTime.ToString(@"hh\:mm"),
            SlotDurationMinutes = s.SlotDurationMinutes,
            IsActive = s.IsActive
        };
    }

    private async Task<List<DoctorScheduleDto>> MapToDtoList(List<DoctorSchedule> schedules)
    {
        var doctorIds = schedules.Select(s => s.DoctorId).Distinct().ToList();
        var departmentIds = schedules.Select(s => s.DepartmentId).Distinct().ToList();

        var doctors = await _context.Users
            .Where(u => doctorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);
        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d);

        return schedules.Select(s => new DoctorScheduleDto
        {
            Id = s.Id,
            DoctorId = s.DoctorId,
            DoctorName = doctors.TryGetValue(s.DoctorId, out var d) ? $"{d.FirstName} {d.LastName}" : "",
            DepartmentId = s.DepartmentId,
            DepartmentName = departments.TryGetValue(s.DepartmentId, out var dept) ? dept.Name : "",
            DayOfWeek = s.DayOfWeek.ToString(),
            StartTime = s.StartTime.ToString(@"hh\:mm"),
            EndTime = s.EndTime.ToString(@"hh\:mm"),
            SlotDurationMinutes = s.SlotDurationMinutes,
            IsActive = s.IsActive
        }).ToList();
    }
}
