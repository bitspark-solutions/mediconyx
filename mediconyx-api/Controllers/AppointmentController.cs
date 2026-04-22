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
public class AppointmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(ApplicationDbContext context, ILogger<AppointmentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<AppointmentListResponse>> GetAppointments([FromQuery] AppointmentQueryParams queryParams)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Appointments
            .Where(a => a.TenantId == tenantId.Value)
            .AsQueryable();

        if (queryParams.PatientId.HasValue)
            query = query.Where(a => a.PatientId == queryParams.PatientId.Value);

        if (queryParams.DoctorId.HasValue)
            query = query.Where(a => a.DoctorId == queryParams.DoctorId.Value);

        if (queryParams.DepartmentId.HasValue)
            query = query.Where(a => a.DepartmentId == queryParams.DepartmentId.Value);

        if (queryParams.Status.HasValue)
            query = query.Where(a => a.Status == queryParams.Status.Value);

        if (queryParams.DateFrom.HasValue)
            query = query.Where(a => a.AppointmentDate >= queryParams.DateFrom.Value);

        if (queryParams.DateTo.HasValue)
            query = query.Where(a => a.AppointmentDate <= queryParams.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var search = queryParams.SearchTerm.ToLower();
            query = query.Where(a =>
                (a.Reason != null && a.Reason.ToLower().Contains(search)) ||
                (a.Notes != null && a.Notes.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        query = queryParams.SortBy?.ToLower() switch
        {
            "appointmentdate" => queryParams.SortOrder == "asc"
                ? query.OrderBy(a => a.AppointmentDate)
                : query.OrderByDescending(a => a.AppointmentDate),
            "status" => queryParams.SortOrder == "asc"
                ? query.OrderBy(a => a.Status)
                : query.OrderByDescending(a => a.Status),
            "createdat" or _ => queryParams.SortOrder == "asc"
                ? query.OrderBy(a => a.CreatedAt)
                : query.OrderByDescending(a => a.CreatedAt)
        };

        var pageSize = Math.Min(queryParams.PageSize, 100);
        var pageNumber = Math.Max(queryParams.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var appointments = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new AppointmentListResponse
        {
            Appointments = await MapToDtoList(appointments),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId.Value);

        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });

        return Ok(await MapToDto(appointment));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(CreateAppointmentRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null) return Unauthorized();

        if (request.PatientId <= 0)
            return BadRequest(new { message = "Patient ID is required." });
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

        var appointment = new Appointment
        {
            TenantId = tenantId.Value,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            DepartmentId = request.DepartmentId,
            AppointmentDate = request.AppointmentDate.Date,
            StartTime = startTime,
            EndTime = endTime,
            Type = request.Type,
            Reason = request.Reason,
            Notes = request.Notes,
            Status = AppointmentStatus.Scheduled,
            CreatedByUserId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment created: {AppointmentId} for Patient {PatientId} in Tenant {TenantId}",
            appointment.Id, appointment.PatientId, tenantId.Value);

        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, await MapToDto(appointment));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, UpdateAppointmentRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId.Value);

        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });

        if (request.DoctorId.HasValue && request.DoctorId.Value > 0)
            appointment.DoctorId = request.DoctorId.Value;
        if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            appointment.DepartmentId = request.DepartmentId.Value;
        if (request.AppointmentDate.HasValue)
            appointment.AppointmentDate = request.AppointmentDate.Value.Date;
        if (!string.IsNullOrWhiteSpace(request.StartTime) && TimeSpan.TryParse(request.StartTime, out var st))
            appointment.StartTime = st;
        if (!string.IsNullOrWhiteSpace(request.EndTime) && TimeSpan.TryParse(request.EndTime, out var et))
            appointment.EndTime = et;
        if (request.Type.HasValue)
            appointment.Type = request.Type.Value;
        if (request.Reason != null)
            appointment.Reason = request.Reason;
        if (request.Notes != null)
            appointment.Notes = request.Notes;

        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment updated: {AppointmentId} by User {UserId}", id, GetCurrentUserId());

        return Ok(await MapToDto(appointment));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointmentStatus(int id, [FromBody] AppointmentStatus status)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId.Value);

        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });

        appointment.Status = status;
        appointment.UpdatedAt = DateTime.UtcNow;

        if (status == AppointmentStatus.CheckedIn)
            appointment.CheckedInAt = DateTime.UtcNow;
        else if (status == AppointmentStatus.Completed)
            appointment.CompletedAt = DateTime.UtcNow;
        else if (status == AppointmentStatus.Cancelled)
            appointment.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment status updated: {AppointmentId} to {Status}", id, status);

        return Ok(await MapToDto(appointment));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<ActionResult<AppointmentDto>> CancelAppointment(int id, CancelAppointmentRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId.Value);

        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.CancelReason = request.Reason;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment cancelled: {AppointmentId} with reason: {Reason}", id, request.Reason);

        return Ok(await MapToDto(appointment));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId.Value);

        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.CancelReason = "Deleted by admin";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment deleted (cancelled): {AppointmentId} by Admin {AdminId}", id, GetCurrentUserId());

        return NoContent();
    }

    [HttpGet("slots")]
    public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots([FromQuery] AvailableSlotsQueryParams queryParams)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var dayOfWeek = queryParams.Date.DayOfWeek;

        var schedules = await _context.DoctorSchedules
            .Where(s => s.TenantId == tenantId.Value
                && s.DoctorId == queryParams.DoctorId
                && s.DayOfWeek == dayOfWeek
                && s.IsActive)
            .ToListAsync();

        var slots = new List<AvailableSlotDto>();

        foreach (var schedule in schedules)
        {
            var start = schedule.StartTime;
            var end = schedule.EndTime;
            var duration = TimeSpan.FromMinutes(schedule.SlotDurationMinutes);

            while (start + duration <= end)
            {
                var slotEnd = start + duration;

                var isBooked = await _context.Appointments
                    .AnyAsync(a => a.TenantId == tenantId.Value
                        && a.DoctorId == queryParams.DoctorId
                        && a.AppointmentDate.Date == queryParams.Date.Date
                        && a.Status != AppointmentStatus.Cancelled
                        && a.StartTime < slotEnd
                        && a.EndTime > start);

                if (!isBooked)
                {
                    slots.Add(new AvailableSlotDto
                    {
                        StartTime = start.ToString(@"hh\:mm"),
                        EndTime = slotEnd.ToString(@"hh\:mm")
                    });
                }

                start = slotEnd;
            }
        }

        return Ok(slots);
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

    private async Task<AppointmentDto> MapToDto(Appointment a)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == a.PatientId);
        var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == a.DoctorId);
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == a.DepartmentId);

        return new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "",
            PatientNumber = patient?.PatientNumber ?? "",
            DoctorId = a.DoctorId,
            DoctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "",
            DepartmentId = a.DepartmentId,
            DepartmentName = department?.Name ?? "",
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime.ToString(@"hh\:mm"),
            EndTime = a.EndTime.ToString(@"hh\:mm"),
            Status = a.Status.ToString(),
            Type = a.Type.ToString(),
            Reason = a.Reason,
            Notes = a.Notes,
            CheckedInAt = a.CheckedInAt,
            CompletedAt = a.CompletedAt,
            CancelledAt = a.CancelledAt,
            CancelReason = a.CancelReason,
            CreatedAt = a.CreatedAt
        };
    }

    private async Task<List<AppointmentDto>> MapToDtoList(List<Appointment> appointments)
    {
        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
        var doctorIds = appointments.Select(a => a.DoctorId).Distinct().ToList();
        var departmentIds = appointments.Select(a => a.DepartmentId).Distinct().ToList();

        var patients = await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);
        var doctors = await _context.Users
            .Where(u => doctorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);
        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d);

        return appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = patients.TryGetValue(a.PatientId, out var p) ? $"{p.FirstName} {p.LastName}" : "",
            PatientNumber = patients.TryGetValue(a.PatientId, out var p2) ? p2.PatientNumber : "",
            DoctorId = a.DoctorId,
            DoctorName = doctors.TryGetValue(a.DoctorId, out var d) ? $"{d.FirstName} {d.LastName}" : "",
            DepartmentId = a.DepartmentId,
            DepartmentName = departments.TryGetValue(a.DepartmentId, out var dept) ? dept.Name : "",
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime.ToString(@"hh\:mm"),
            EndTime = a.EndTime.ToString(@"hh\:mm"),
            Status = a.Status.ToString(),
            Type = a.Type.ToString(),
            Reason = a.Reason,
            Notes = a.Notes,
            CheckedInAt = a.CheckedInAt,
            CompletedAt = a.CompletedAt,
            CancelledAt = a.CancelledAt,
            CancelReason = a.CancelReason,
            CreatedAt = a.CreatedAt
        }).ToList();
    }
}