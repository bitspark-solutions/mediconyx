using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Appointments;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(ApplicationDbContext context, ILogger<AppointmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AppointmentListResponse> GetAppointmentsAsync(int tenantId, AppointmentQueryParams queryParams)
    {
        var query = _context.Appointments
            .Where(a => a.TenantId == tenantId)
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
            _ => queryParams.SortOrder == "asc"
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

        return new AppointmentListResponse
        {
            Appointments = await MapToDtoListAsync(appointments),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int tenantId, int appointmentId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.TenantId == tenantId);

        return appointment is null ? null : await MapToDtoAsync(appointment);
    }

    public async Task<AppointmentDto> CreateAppointmentAsync(int tenantId, int createdByUserId, CreateAppointmentRequest request)
    {
        if (!TimeSpan.TryParse(request.StartTime, out var startTime))
            throw new ArgumentException("Invalid start time format.");
        if (!TimeSpan.TryParse(request.EndTime, out var endTime))
            throw new ArgumentException("Invalid end time format.");
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time.");

        var appointment = new Appointment
        {
            TenantId = tenantId,
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
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment created: {AppointmentId} for Patient {PatientId} in Tenant {TenantId}",
            appointment.Id, appointment.PatientId, tenantId);

        return await MapToDtoAsync(appointment);
    }

    public async Task<AppointmentDto> UpdateAppointmentAsync(int tenantId, int appointmentId, UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

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

        _logger.LogInformation("Appointment updated: {AppointmentId} in Tenant {TenantId}", appointmentId, tenantId);

        return await MapToDtoAsync(appointment);
    }

    public async Task<AppointmentDto> UpdateStatusAsync(int tenantId, int appointmentId, AppointmentStatus status)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

        appointment.Status = status;
        appointment.UpdatedAt = DateTime.UtcNow;

        if (status == AppointmentStatus.CheckedIn)
            appointment.CheckedInAt = DateTime.UtcNow;
        else if (status == AppointmentStatus.Completed)
            appointment.CompletedAt = DateTime.UtcNow;
        else if (status == AppointmentStatus.Cancelled)
            appointment.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment {AppointmentId} status updated to {Status}", appointmentId, status);

        return await MapToDtoAsync(appointment);
    }

    public async Task<AppointmentDto> CancelAppointmentAsync(int tenantId, int appointmentId, string reason)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.CancelReason = reason;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment {AppointmentId} cancelled. Reason: {Reason}", appointmentId, reason);

        return await MapToDtoAsync(appointment);
    }

    public async Task DeleteAppointmentAsync(int tenantId, int appointmentId, int adminUserId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.CancelReason = "Deleted by admin";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment {AppointmentId} deleted by Admin {AdminId}", appointmentId, adminUserId);
    }

    public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(int tenantId, AvailableSlotsQueryParams queryParams)
    {
        var dayOfWeek = queryParams.Date.DayOfWeek;

        var schedules = await _context.DoctorSchedules
            .Where(s => s.TenantId == tenantId
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
                    .AnyAsync(a => a.TenantId == tenantId
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

        return slots;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<AppointmentDto> MapToDtoAsync(Appointment a)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == a.PatientId);
        var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == a.DoctorId);
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == a.DepartmentId);

        return BuildDto(a, patient, doctor, department);
    }

    private async Task<List<AppointmentDto>> MapToDtoListAsync(List<Appointment> appointments)
    {
        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
        var doctorIds = appointments.Select(a => a.DoctorId).Distinct().ToList();
        var departmentIds = appointments.Select(a => a.DepartmentId).Distinct().ToList();

        var patients = await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        var doctors = await _context.Users
            .Where(u => doctorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);
        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id);

        return appointments.Select(a => BuildDto(
            a,
            patients.GetValueOrDefault(a.PatientId),
            doctors.GetValueOrDefault(a.DoctorId),
            departments.GetValueOrDefault(a.DepartmentId)
        )).ToList();
    }

    private static AppointmentDto BuildDto(Appointment a, Patient? patient, User? doctor, Department? department) => new()
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
