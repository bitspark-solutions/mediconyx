using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Middleware;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using System.Security.Claims;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PrescriptionController> _logger;

    public PrescriptionController(ApplicationDbContext context, ILogger<PrescriptionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PrescriptionListResponse>> GetPrescriptions(
        [FromQuery] int? patientId,
        [FromQuery] int? appointmentId,
        [FromQuery] int? doctorId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Prescriptions
            .Where(p => p.TenantId == tenantId.Value)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(p => p.PatientId == patientId.Value);

        if (appointmentId.HasValue)
            query = query.Where(p => p.AppointmentId == appointmentId.Value);

        if (doctorId.HasValue)
            query = query.Where(p => p.DoctorId == doctorId.Value);

        var totalCount = await query.CountAsync();

        var pageSizeClamped = Math.Min(pageSize, 100);
        var pageNumberClamped = Math.Max(pageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSizeClamped);

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumberClamped - 1) * pageSizeClamped)
            .Take(pageSizeClamped)
            .ToListAsync();

        return Ok(new PrescriptionListResponse
        {
            Prescriptions = await MapToDtoList(prescriptions),
            TotalCount = totalCount,
            PageNumber = pageNumberClamped,
            PageSize = pageSizeClamped,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PrescriptionDto>> GetPrescription(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var prescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (prescription == null)
            return NotFound(new { message = "Prescription not found." });

        return Ok(await MapToDto(prescription));
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<PrescriptionListResponse>> GetPrescriptionsByPatient(
        int patientId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Prescriptions
            .Where(p => p.TenantId == tenantId.Value && p.PatientId == patientId)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var pageSizeClamped = Math.Min(pageSize, 100);
        var pageNumberClamped = Math.Max(pageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSizeClamped);

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumberClamped - 1) * pageSizeClamped)
            .Take(pageSizeClamped)
            .ToListAsync();

        return Ok(new PrescriptionListResponse
        {
            Prescriptions = await MapToDtoList(prescriptions),
            TotalCount = totalCount,
            PageNumber = pageNumberClamped,
            PageSize = pageSizeClamped,
            TotalPages = totalPages
        });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.PRESCRIPTION_CREATE)]
    public async Task<ActionResult<PrescriptionDto>> CreatePrescription(CreatePrescriptionRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null) return Unauthorized();

        if (request.PatientId <= 0)
            return BadRequest(new { message = "Patient ID is required." });
        if (request.AppointmentId <= 0)
            return BadRequest(new { message = "Appointment ID is required." });
        if (string.IsNullOrWhiteSpace(request.Medications))
            return BadRequest(new { message = "Medications are required." });

        var prescription = new Prescription
        {
            TenantId = tenantId.Value,
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            DoctorId = userId.Value,
            Medications = request.Medications.Trim(),
            Instructions = request.Instructions?.Trim(),
            Allergies = request.Allergies?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Prescription created: {PrescriptionId} for Patient {PatientId} by Doctor {DoctorId}",
            prescription.Id, prescription.PatientId, userId.Value);

        return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, await MapToDto(prescription));
    }

    [HttpPut("{id}")]
    [HasPermission(PermissionCatalog.PRESCRIPTION_CREATE)]
    public async Task<ActionResult<PrescriptionDto>> UpdatePrescription(int id, UpdatePrescriptionRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null) return Unauthorized();

        var prescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (prescription == null)
            return NotFound(new { message = "Prescription not found." });

        if (!string.IsNullOrWhiteSpace(request.Medications))
            prescription.Medications = request.Medications.Trim();
        if (request.Instructions != null)
            prescription.Instructions = request.Instructions.Trim();
        if (request.Allergies != null)
            prescription.Allergies = request.Allergies.Trim();

        prescription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Prescription updated: {PrescriptionId} by User {UserId}", id, userId.Value);

        return Ok(await MapToDto(prescription));
    }

    [HttpDelete("{id}")]
    [HasPermission(PermissionCatalog.PRESCRIPTION_CREATE)]
    public async Task<IActionResult> DeletePrescription(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var prescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (prescription == null)
            return NotFound(new { message = "Prescription not found." });

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Prescription deleted: {PrescriptionId} by Admin {AdminId}", id, GetCurrentUserId());

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

    private async Task<PrescriptionDto> MapToDto(Prescription p)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(pt => pt.Id == p.PatientId);
        var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == p.DoctorId);

        return new PrescriptionDto
        {
            Id = p.Id,
            PatientId = p.PatientId,
            PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "",
            AppointmentId = p.AppointmentId,
            DoctorId = p.DoctorId,
            DoctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "",
            Medications = p.Medications,
            Instructions = p.Instructions,
            Allergies = p.Allergies,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }

    private async Task<List<PrescriptionDto>> MapToDtoList(List<Prescription> prescriptions)
    {
        var patientIds = prescriptions.Select(p => p.PatientId).Distinct().ToList();
        var doctorIds = prescriptions.Select(p => p.DoctorId).Distinct().ToList();

        var patients = await _context.Patients
            .Where(pt => patientIds.Contains(pt.Id))
            .ToDictionaryAsync(pt => pt.Id, pt => pt);
        var doctors = await _context.Users
            .Where(u => doctorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        return prescriptions.Select(p => new PrescriptionDto
        {
            Id = p.Id,
            PatientId = p.PatientId,
            PatientName = patients.TryGetValue(p.PatientId, out var pt) ? $"{pt.FirstName} {pt.LastName}" : "",
            AppointmentId = p.AppointmentId,
            DoctorId = p.DoctorId,
            DoctorName = doctors.TryGetValue(p.DoctorId, out var d) ? $"{d.FirstName} {d.LastName}" : "",
            Medications = p.Medications,
            Instructions = p.Instructions,
            Allergies = p.Allergies,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }
}