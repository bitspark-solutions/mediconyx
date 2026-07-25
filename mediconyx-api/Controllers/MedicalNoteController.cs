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
[Route("api/medical-note")]
[Authorize]
public class MedicalNoteController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicalNoteController> _logger;

    public MedicalNoteController(ApplicationDbContext context, ILogger<MedicalNoteController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<MedicalNoteListResponse>> GetNotes(
        [FromQuery] int? patientId,
        [FromQuery] int? appointmentId,
        [FromQuery] MedicalNoteType? noteType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.MedicalNotes
            .Where(n => n.TenantId == tenantId.Value)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(n => n.PatientId == patientId.Value);

        if (appointmentId.HasValue)
            query = query.Where(n => n.AppointmentId == appointmentId.Value);

        if (noteType.HasValue)
            query = query.Where(n => n.NoteType == noteType.Value);

        var totalCount = await query.CountAsync();

        var pageSizeClamped = Math.Min(pageSize, 100);
        var pageNumberClamped = Math.Max(pageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSizeClamped);

        var notes = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumberClamped - 1) * pageSizeClamped)
            .Take(pageSizeClamped)
            .ToListAsync();

        return Ok(new MedicalNoteListResponse
        {
            Notes = await MapToDtoList(notes),
            TotalCount = totalCount,
            PageNumber = pageNumberClamped,
            PageSize = pageSizeClamped,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MedicalNoteDto>> GetNote(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var note = await _context.MedicalNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId.Value);

        if (note == null)
            return NotFound(new { message = "Medical note not found." });

        return Ok(await MapToDto(note));
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<MedicalNoteListResponse>> GetNotesByPatient(
        int patientId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.MedicalNotes
            .Where(n => n.TenantId == tenantId.Value && n.PatientId == patientId)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var pageSizeClamped = Math.Min(pageSize, 100);
        var pageNumberClamped = Math.Max(pageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSizeClamped);

        var notes = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumberClamped - 1) * pageSizeClamped)
            .Take(pageSizeClamped)
            .ToListAsync();

        return Ok(new MedicalNoteListResponse
        {
            Notes = await MapToDtoList(notes),
            TotalCount = totalCount,
            PageNumber = pageNumberClamped,
            PageSize = pageSizeClamped,
            TotalPages = totalPages
        });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.MEDICAL_NOTE_CREATE)]
    public async Task<ActionResult<MedicalNoteDto>> CreateNote(CreateMedicalNoteRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null) return Unauthorized();

        if (request.PatientId <= 0)
            return BadRequest(new { message = "Patient ID is required." });
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "Content is required." });

        var note = new MedicalNote
        {
            TenantId = tenantId.Value,
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId > 0 ? request.AppointmentId : null,
            AuthorUserId = userId.Value,
            NoteType = request.NoteType,
            Content = request.Content.Trim(),
            Diagnosis = request.Diagnosis?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.MedicalNotes.Add(note);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Medical note created: {NoteId} for Patient {PatientId} by User {UserId}",
            note.Id, note.PatientId, userId.Value);

        return CreatedAtAction(nameof(GetNote), new { id = note.Id }, await MapToDto(note));
    }

    [HttpPut("{id}")]
    [HasPermission(PermissionCatalog.MEDICAL_NOTE_CREATE)]
    public async Task<ActionResult<MedicalNoteDto>> UpdateNote(int id, UpdateMedicalNoteRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null) return Unauthorized();

        var note = await _context.MedicalNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId.Value);

        if (note == null)
            return NotFound(new { message = "Medical note not found." });

        if (request.NoteType.HasValue)
            note.NoteType = request.NoteType.Value;
        if (!string.IsNullOrWhiteSpace(request.Content))
            note.Content = request.Content.Trim();
        if (request.Diagnosis != null)
            note.Diagnosis = request.Diagnosis.Trim();

        note.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Medical note updated: {NoteId} by User {UserId}", id, userId.Value);

        return Ok(await MapToDto(note));
    }

    [HttpDelete("{id}")]
    [HasPermission(PermissionCatalog.MEDICAL_NOTE_CREATE)]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var note = await _context.MedicalNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId.Value);

        if (note == null)
            return NotFound(new { message = "Medical note not found." });

        _context.MedicalNotes.Remove(note);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Medical note deleted: {NoteId} by Admin {AdminId}", id, GetCurrentUserId());

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

    private async Task<MedicalNoteDto> MapToDto(MedicalNote n)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == n.PatientId);
        var author = await _context.Users.FirstOrDefaultAsync(u => u.Id == n.AuthorUserId);

        return new MedicalNoteDto
        {
            Id = n.Id,
            PatientId = n.PatientId,
            PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "",
            AppointmentId = n.AppointmentId,
            AuthorUserId = n.AuthorUserId,
            AuthorName = author != null ? $"{author.FirstName} {author.LastName}" : "",
            NoteType = n.NoteType.ToString(),
            Content = n.Content,
            Diagnosis = n.Diagnosis,
            CreatedAt = n.CreatedAt,
            UpdatedAt = n.UpdatedAt
        };
    }

    private async Task<List<MedicalNoteDto>> MapToDtoList(List<MedicalNote> notes)
    {
        var patientIds = notes.Select(n => n.PatientId).Distinct().ToList();
        var authorIds = notes.Select(n => n.AuthorUserId).Distinct().ToList();

        var patients = await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);
        var authors = await _context.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        return notes.Select(n => new MedicalNoteDto
        {
            Id = n.Id,
            PatientId = n.PatientId,
            PatientName = patients.TryGetValue(n.PatientId, out var p) ? $"{p.FirstName} {p.LastName}" : "",
            AppointmentId = n.AppointmentId,
            AuthorUserId = n.AuthorUserId,
            AuthorName = authors.TryGetValue(n.AuthorUserId, out var a) ? $"{a.FirstName} {a.LastName}" : "",
            NoteType = n.NoteType.ToString(),
            Content = n.Content,
            Diagnosis = n.Diagnosis,
            CreatedAt = n.CreatedAt,
            UpdatedAt = n.UpdatedAt
        }).ToList();
    }
}
