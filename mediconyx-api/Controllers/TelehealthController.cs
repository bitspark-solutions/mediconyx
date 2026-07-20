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
/// Telemedicine — MVP stub. Manages session lifecycle (Scheduled → Active → Completed).
/// Video transport is a placeholder: JoinUrl is a fabricated room URL; swap in a real
/// provider (Twilio Video, Daily.co, Jitsi) by overriding the URL generation in CreateAsync.
/// </summary>
[ApiController]
[Route("api/telemedicine")]
[Authorize]
public class TelehealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TelehealthController> _logger;

    public TelehealthController(ApplicationDbContext context, ILogger<TelehealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int? TenantId => ClaimsHelper.GetTenantId(User);
    private int? UserId => ClaimsHelper.GetUserId(User);

    [HttpGet("sessions")]
    [HasPermission(PermissionCatalog.TELEMEDICINE_START)]
    public async Task<ActionResult<List<TelehealthSessionDto>>> GetSessions(
        [FromQuery] string? status = null,
        [FromQuery] int? patientId = null,
        [FromQuery] int? doctorId = null,
        [FromQuery] bool upcomingOnly = false)
    {
        if (TenantId == null) return Unauthorized();
        var q = _context.TelehealthSessions
            .Include(s => s.Patient)
            .Include(s => s.Doctor).ThenInclude(d => d.User)
            .Where(s => s.TenantId == TenantId);
        if (patientId.HasValue) q = q.Where(s => s.PatientId == patientId.Value);
        if (doctorId.HasValue) q = q.Where(s => s.DoctorId == doctorId.Value);
        if (upcomingOnly) q = q.Where(s => s.ScheduledAt >= DateTime.UtcNow && s.Status == TelehealthSessionStatus.Scheduled);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TelehealthSessionStatus>(status, out var st))
            q = q.Where(s => s.Status == st);

        var list = await q.OrderByDescending(s => s.ScheduledAt).ToListAsync();
        return Ok(list.Select(MapSession).ToList());
    }

    [HttpGet("sessions/{id}")]
    [HasPermission(PermissionCatalog.TELEMEDICINE_JOIN)]
    public async Task<ActionResult<TelehealthSessionDto>> GetSession(int id)
    {
        if (TenantId == null) return Unauthorized();
        var s = await _context.TelehealthSessions
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
        if (s == null) return NotFound();
        return Ok(MapSession(s));
    }

    [HttpPost("sessions")]
    [HasPermission(PermissionCatalog.TELEMEDICINE_START)]
    public async Task<ActionResult<TelehealthSessionDto>> CreateSession(CreateTelehealthSessionRequest req)
    {
        if (TenantId == null) return Unauthorized();
        if (UserId == null) return Unauthorized();

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == req.PatientId && p.TenantId == TenantId);
        if (patient == null) return BadRequest(new { message = "Patient not found in this tenant." });

        var doctor = await _context.DoctorProfiles.Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == req.DoctorId && d.TenantId == TenantId);
        if (doctor == null) return BadRequest(new { message = "Doctor not found in this tenant." });

        // MVP: fabricate a room URL. Real provider integration replaces this.
        var roomId = Guid.NewGuid().ToString("N").Substring(0, 12);
        var joinToken = Guid.NewGuid().ToString("N");

        var session = new TelehealthSession
        {
            TenantId = TenantId.Value,
            PatientId = req.PatientId,
            DoctorId = req.DoctorId,
            AppointmentId = req.AppointmentId,
            ScheduledAt = req.ScheduledAt,
            Status = TelehealthSessionStatus.Scheduled,
            JoinUrl = $"https://meet.mediconyx.local/room/{roomId}",
            JoinToken = joinToken,
            Notes = req.Notes,
            CreatedByUserId = UserId.Value,
            CreatedAt = DateTime.UtcNow
        };
        _context.TelehealthSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Telemedicine session {Id} created for patient {PatientId} and doctor {DoctorId}", session.Id, req.PatientId, req.DoctorId);

        var fresh = await _context.TelehealthSessions
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(d => d.User)
            .FirstAsync(x => x.Id == session.Id);
        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, MapSession(fresh));
    }

    [HttpPost("sessions/{id}/start")]
    [HasPermission(PermissionCatalog.TELEMEDICINE_START)]
    public async Task<ActionResult<TelehealthSessionDto>> StartSession(int id)
    {
        if (TenantId == null) return Unauthorized();
        var s = await _context.TelehealthSessions
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
        if (s == null) return NotFound();
        if (s.Status != TelehealthSessionStatus.Scheduled)
            return BadRequest(new { message = $"Cannot start a session in {s.Status} status." });

        s.Status = TelehealthSessionStatus.Active;
        s.StartedAt = DateTime.UtcNow;
        s.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(MapSession(s));
    }

    [HttpPost("sessions/{id}/end")]
    [HasPermission(PermissionCatalog.TELEMEDICINE_START)]
    public async Task<ActionResult<TelehealthSessionDto>> EndSession(int id, [FromBody] string? notes = null)
    {
        if (TenantId == null) return Unauthorized();
        var s = await _context.TelehealthSessions
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
        if (s == null) return NotFound();
        if (s.Status == TelehealthSessionStatus.Completed || s.Status == TelehealthSessionStatus.Cancelled)
            return BadRequest(new { message = $"Session already {s.Status}." });

        s.Status = TelehealthSessionStatus.Completed;
        s.EndedAt = DateTime.UtcNow;
        if (s.StartedAt == null) s.StartedAt = s.EndedAt;
        if (!string.IsNullOrWhiteSpace(notes)) s.Notes = (s.Notes != null ? s.Notes + "\n\n" : "") + notes;
        s.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(MapSession(s));
    }

    [HttpPost("sessions/{id}/cancel")]
    [HasPermission(PermissionCatalog.TELEMEDICINE_START)]
    public async Task<ActionResult<TelehealthSessionDto>> CancelSession(int id, [FromBody] string? reason = null)
    {
        if (TenantId == null) return Unauthorized();
        var s = await _context.TelehealthSessions
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
        if (s == null) return NotFound();
        if (s.Status != TelehealthSessionStatus.Scheduled)
            return BadRequest(new { message = $"Cannot cancel a session in {s.Status} status." });

        s.Status = TelehealthSessionStatus.Cancelled;
        s.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason)) s.Notes = (s.Notes != null ? s.Notes + "\n\n" : "") + $"Cancelled: {reason}";
        await _context.SaveChangesAsync();
        return Ok(MapSession(s));
    }

    private static TelehealthSessionDto MapSession(TelehealthSession s) => new()
    {
        Id = s.Id,
        TenantId = s.TenantId,
        PatientId = s.PatientId,
        PatientName = $"{s.Patient.FirstName} {s.Patient.LastName}",
        PatientNumber = s.Patient.PatientNumber,
        DoctorId = s.DoctorId,
        DoctorName = $"Dr. {s.Doctor.User.FirstName} {s.Doctor.User.LastName}",
        AppointmentId = s.AppointmentId,
        ScheduledAt = s.ScheduledAt,
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt,
        Status = s.Status.ToString(),
        JoinUrl = s.JoinUrl,
        JoinToken = s.JoinToken,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt
    };
}
