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
public class PatientController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PatientController> _logger;

    public PatientController(ApplicationDbContext context, ILogger<PatientController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List patients with filtering and pagination (tenant-scoped)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PatientListResponse>> GetPatients([FromQuery] PatientQueryParams queryParams)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Patients.Where(p => p.TenantId == tenantId.Value).AsQueryable();

        // Filter by active status
        if (queryParams.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == queryParams.IsActive.Value);
        }

        // Filter by gender
        if (queryParams.Gender.HasValue)
        {
            query = query.Where(p => p.Gender == queryParams.Gender.Value);
        }

        // Filter by blood group
        if (!string.IsNullOrWhiteSpace(queryParams.BloodGroup))
        {
            query = query.Where(p => p.BloodGroup == queryParams.BloodGroup);
        }

        // Search term (name, phone, patient number, national ID)
        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var search = queryParams.SearchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(search) ||
                p.LastName.ToLower().Contains(search) ||
                p.Phone.Contains(search) ||
                p.PatientNumber.ToLower().Contains(search) ||
                (p.NationalId != null && p.NationalId.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        // Sorting
        query = queryParams.SortBy?.ToLower() switch
        {
            "lastname" => queryParams.SortOrder == "asc"
                ? query.OrderBy(p => p.LastName)
                : query.OrderByDescending(p => p.LastName),
            "patientnumber" => queryParams.SortOrder == "asc"
                ? query.OrderBy(p => p.PatientNumber)
                : query.OrderByDescending(p => p.PatientNumber),
            "createdat" or _ => queryParams.SortOrder == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt)
        };

        // Pagination
        var pageSize = Math.Min(queryParams.PageSize, 100);
        var pageNumber = Math.Max(queryParams.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var patients = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new PatientListResponse
        {
            Patients = patients.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return Ok(response);
    }

    /// <summary>
    /// Get single patient by ID (tenant-scoped)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PatientDto>> GetPatient(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (patient == null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        return Ok(MapToDto(patient));
    }

    /// <summary>
    /// Create a new patient (tenant-scoped, auto-generates PatientNumber)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor,Receptionist,Nurse")]
    public async Task<ActionResult<PatientDto>> CreatePatient(CreatePatientRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null) return Unauthorized();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.FirstName))
            return BadRequest(new { message = "First name is required." });
        if (string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "Last name is required." });
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Phone number is required." });

        // Auto-generate patient number
        var maxNumber = await _context.Patients
            .Where(p => p.TenantId == tenantId.Value)
            .OrderByDescending(p => p.PatientNumber)
            .Select(p => p.PatientNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (maxNumber != null && maxNumber.StartsWith("P-") && int.TryParse(maxNumber[2..], out var current))
        {
            nextSeq = current + 1;
        }
        var patientNumber = $"P-{nextSeq:D6}";

        var patient = new Patient
        {
            TenantId = tenantId.Value,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            BloodGroup = request.BloodGroup,
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim(),
            NationalId = request.NationalId?.Trim(),
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            EmergencyContactName = request.EmergencyContactName?.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
            EmergencyContactRelation = request.EmergencyContactRelation?.Trim(),
            PatientNumber = patientNumber,
            IsActive = true,
            CreatedByUserId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Patient created: {PatientNumber} in Tenant {TenantId} by User {UserId}",
            patient.PatientNumber, tenantId.Value, userId.Value);

        return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, MapToDto(patient));
    }

    /// <summary>
    /// Update patient (tenant-scoped)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<ActionResult<PatientDto>> UpdatePatient(int id, UpdatePatientRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (patient == null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(request.FirstName)) patient.FirstName = request.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LastName)) patient.LastName = request.LastName.Trim();
        if (request.DateOfBirth.HasValue) patient.DateOfBirth = request.DateOfBirth.Value;
        if (request.Gender.HasValue) patient.Gender = request.Gender.Value;
        if (request.BloodGroup != null) patient.BloodGroup = request.BloodGroup;
        if (request.Phone != null) patient.Phone = request.Phone.Trim();
        if (request.Email != null) patient.Email = request.Email.Trim();
        if (request.NationalId != null) patient.NationalId = request.NationalId.Trim();
        if (request.Address != null) patient.Address = request.Address.Trim();
        if (request.City != null) patient.City = request.City.Trim();
        if (request.PostalCode != null) patient.PostalCode = request.PostalCode.Trim();
        if (request.EmergencyContactName != null) patient.EmergencyContactName = request.EmergencyContactName.Trim();
        if (request.EmergencyContactPhone != null) patient.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
        if (request.EmergencyContactRelation != null) patient.EmergencyContactRelation = request.EmergencyContactRelation.Trim();
        if (request.IsActive.HasValue) patient.IsActive = request.IsActive.Value;

        patient.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Patient updated: {PatientId} by User {UserId}", id, GetCurrentUserId());

        return Ok(MapToDto(patient));
    }

    /// <summary>
    /// Soft-delete patient (Admin only, tenant-scoped)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (patient == null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        patient.IsActive = false;
        patient.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Patient deactivated: {PatientId} by Admin {AdminId}", id, GetCurrentUserId());

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

    private static PatientDto MapToDto(Patient p)
    {
        return new PatientDto
        {
            Id = p.Id,
            PatientNumber = p.PatientNumber,
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            Gender = p.Gender.ToString(),
            BloodGroup = p.BloodGroup,
            Phone = p.Phone,
            Email = p.Email,
            NationalId = p.NationalId,
            Address = p.Address,
            City = p.City,
            PostalCode = p.PostalCode,
            EmergencyContactName = p.EmergencyContactName,
            EmergencyContactPhone = p.EmergencyContactPhone,
            EmergencyContactRelation = p.EmergencyContactRelation,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
