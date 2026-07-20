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
[Route("api/[controller]")]
[Authorize]
public class VitalSignController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VitalSignController> _logger;

    public VitalSignController(ApplicationDbContext context, ILogger<VitalSignController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<VitalSignListResponse>> GetVitalSigns([FromQuery] VitalSignQueryParams queryParams)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.VitalSigns
            .Where(v => v.TenantId == tenantId.Value)
            .AsQueryable();

        if (queryParams.PatientId.HasValue)
            query = query.Where(v => v.PatientId == queryParams.PatientId.Value);

        if (queryParams.AppointmentId.HasValue)
            query = query.Where(v => v.AppointmentId == queryParams.AppointmentId.Value);

        if (queryParams.DateFrom.HasValue)
            query = query.Where(v => v.RecordedAt >= queryParams.DateFrom.Value);

        if (queryParams.DateTo.HasValue)
            query = query.Where(v => v.RecordedAt <= queryParams.DateTo.Value);

        var totalCount = await query.CountAsync();

        var pageSize = Math.Min(queryParams.PageSize, 100);
        var pageNumber = Math.Max(queryParams.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var vitalSigns = await query
            .OrderByDescending(v => v.RecordedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new VitalSignListResponse
        {
            VitalSigns = await MapToDtoList(vitalSigns),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VitalSignDto>> GetVitalSign(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var vitalSign = await _context.VitalSigns
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId.Value);

        if (vitalSign == null)
            return NotFound(new { message = "Vital sign record not found." });

        return Ok(await MapToDto(vitalSign));
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<VitalSignListResponse>> GetVitalSignsByPatient(
        int patientId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.VitalSigns
            .Where(v => v.TenantId == tenantId.Value && v.PatientId == patientId)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var pageSizeClamped = Math.Min(pageSize, 100);
        var pageNumberClamped = Math.Max(pageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSizeClamped);

        var vitalSigns = await query
            .OrderByDescending(v => v.RecordedAt)
            .Skip((pageNumberClamped - 1) * pageSizeClamped)
            .Take(pageSizeClamped)
            .ToListAsync();

        return Ok(new VitalSignListResponse
        {
            VitalSigns = await MapToDtoList(vitalSigns),
            TotalCount = totalCount,
            PageNumber = pageNumberClamped,
            PageSize = pageSizeClamped,
            TotalPages = totalPages
        });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.VITAL_SIGN_RECORD)]
    public async Task<ActionResult<VitalSignDto>> CreateVitalSign(CreateVitalSignRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null) return Unauthorized();

        if (request.PatientId <= 0)
            return BadRequest(new { message = "Patient ID is required." });

        if (!request.TemperatureCelsius.HasValue
            && !request.BloodPressureSystolic.HasValue
            && !request.BloodPressureDiastolic.HasValue
            && !request.HeartRate.HasValue
            && !request.RespiratoryRate.HasValue
            && !request.WeightKg.HasValue
            && !request.HeightCm.HasValue
            && !request.OxygenSaturation.HasValue
            && !request.BloodGlucose.HasValue)
        {
            return BadRequest(new { message = "At least one vital sign measurement is required." });
        }

        if (request.TemperatureCelsius.HasValue && (request.TemperatureCelsius < 30 || request.TemperatureCelsius > 45))
            return BadRequest(new { message = "Temperature must be between 30 and 45 Celsius." });

        if (request.BloodPressureSystolic.HasValue && (request.BloodPressureSystolic < 50 || request.BloodPressureSystolic > 300))
            return BadRequest(new { message = "Systolic BP must be between 50 and 300." });

        if (request.BloodPressureDiastolic.HasValue && (request.BloodPressureDiastolic < 30 || request.BloodPressureDiastolic > 200))
            return BadRequest(new { message = "Diastolic BP must be between 30 and 200." });

        if (request.HeartRate.HasValue && (request.HeartRate < 20 || request.HeartRate > 300))
            return BadRequest(new { message = "Heart rate must be between 20 and 300." });

        var vitalSign = new VitalSign
        {
            TenantId = tenantId.Value,
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId > 0 ? request.AppointmentId : null,
            RecordedByUserId = userId.Value,
            TemperatureCelsius = request.TemperatureCelsius,
            BloodPressureSystolic = request.BloodPressureSystolic,
            BloodPressureDiastolic = request.BloodPressureDiastolic,
            HeartRate = request.HeartRate,
            RespiratoryRate = request.RespiratoryRate,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            OxygenSaturation = request.OxygenSaturation,
            BloodGlucose = request.BloodGlucose,
            Notes = request.Notes?.Trim(),
            RecordedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.VitalSigns.Add(vitalSign);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vital sign recorded: {VitalSignId} for Patient {PatientId} by User {UserId}",
            vitalSign.Id, vitalSign.PatientId, userId.Value);

        return CreatedAtAction(nameof(GetVitalSign), new { id = vitalSign.Id }, await MapToDto(vitalSign));
    }

    [HttpDelete("{id}")]
    [HasPermission(PermissionCatalog.VITAL_SIGN_RECORD)]
    public async Task<IActionResult> DeleteVitalSign(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var vitalSign = await _context.VitalSigns
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId.Value);

        if (vitalSign == null)
            return NotFound(new { message = "Vital sign record not found." });

        _context.VitalSigns.Remove(vitalSign);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vital sign deleted: {VitalSignId} by Admin {AdminId}", id, GetCurrentUserId());

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

    private async Task<VitalSignDto> MapToDto(VitalSign v)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == v.PatientId);
        var recordedBy = await _context.Users.FirstOrDefaultAsync(u => u.Id == v.RecordedByUserId);

        return new VitalSignDto
        {
            Id = v.Id,
            PatientId = v.PatientId,
            PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "",
            AppointmentId = v.AppointmentId,
            RecordedByUserId = v.RecordedByUserId,
            RecordedByName = recordedBy != null ? $"{recordedBy.FirstName} {recordedBy.LastName}" : "",
            TemperatureCelsius = v.TemperatureCelsius,
            BloodPressureSystolic = v.BloodPressureSystolic,
            BloodPressureDiastolic = v.BloodPressureDiastolic,
            HeartRate = v.HeartRate,
            RespiratoryRate = v.RespiratoryRate,
            WeightKg = v.WeightKg,
            HeightCm = v.HeightCm,
            OxygenSaturation = v.OxygenSaturation,
            BloodGlucose = v.BloodGlucose,
            Notes = v.Notes,
            RecordedAt = v.RecordedAt
        };
    }

    private async Task<List<VitalSignDto>> MapToDtoList(List<VitalSign> vitalSigns)
    {
        var patientIds = vitalSigns.Select(v => v.PatientId).Distinct().ToList();
        var recorderIds = vitalSigns.Select(v => v.RecordedByUserId).Distinct().ToList();

        var patients = await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);
        var recorders = await _context.Users
            .Where(u => recorderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        return vitalSigns.Select(v => new VitalSignDto
        {
            Id = v.Id,
            PatientId = v.PatientId,
            PatientName = patients.TryGetValue(v.PatientId, out var p) ? $"{p.FirstName} {p.LastName}" : "",
            AppointmentId = v.AppointmentId,
            RecordedByUserId = v.RecordedByUserId,
            RecordedByName = recorders.TryGetValue(v.RecordedByUserId, out var r) ? $"{r.FirstName} {r.LastName}" : "",
            TemperatureCelsius = v.TemperatureCelsius,
            BloodPressureSystolic = v.BloodPressureSystolic,
            BloodPressureDiastolic = v.BloodPressureDiastolic,
            HeartRate = v.HeartRate,
            RespiratoryRate = v.RespiratoryRate,
            WeightKg = v.WeightKg,
            HeightCm = v.HeightCm,
            OxygenSaturation = v.OxygenSaturation,
            BloodGlucose = v.BloodGlucose,
            Notes = v.Notes,
            RecordedAt = v.RecordedAt
        }).ToList();
    }
}
