using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Controllers;

/// <summary>
/// Public doctor directory — no auth required. Powers the public
/// /doctors (list) and /doctors/{id} (profile) pages so patients can
/// find and pick a doctor before signing up.
/// </summary>
[ApiController]
[Route("api/public/doctors")]
[AllowAnonymous]
public class PublicDoctorController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PublicDoctorController> _logger;

    public PublicDoctorController(ApplicationDbContext context, ILogger<PublicDoctorController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List doctors across all active tenants. Optional filters:
    ///   ?specialization=Cardiology
    ///   ?tenantId=2
    ///   ?tenantSubdomain=citygeneral
    ///   ?q=hossain  (search name)
    ///   ?accepting=true
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PublicDoctorListResponse>> GetDoctors(
        [FromQuery] string? specialization = null,
        [FromQuery] int? tenantId = null,
        [FromQuery] string? tenantSubdomain = null,
        [FromQuery] string? q = null,
        [FromQuery] bool? accepting = null,
        [FromQuery] int limit = 50)
    {
        if (limit < 1 || limit > 200) limit = 50;

        var query = _context.DoctorProfiles
            .Include(d => d.User)
            .Include(d => d.Department)
            .Include(d => d.Tenant)
            .Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(specialization))
            query = query.Where(d => d.Specialization == specialization);
        if (tenantId.HasValue)
            query = query.Where(d => d.TenantId == tenantId.Value);
        if (!string.IsNullOrWhiteSpace(tenantSubdomain))
            query = query.Where(d => d.Tenant.Subdomain == tenantSubdomain);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(d =>
                d.User.FirstName.ToLower().Contains(needle) ||
                d.User.LastName.ToLower().Contains(needle) ||
                (d.User.Designation != null && d.User.Designation.ToLower().Contains(needle)) ||
                d.Specialization.ToLower().Contains(needle));
        }
        if (accepting == true)
            query = query.Where(d => d.IsAcceptingPatients);

        var doctors = await query
            .OrderBy(d => d.TenantId)
            .ThenBy(d => d.User.LastName)
            .Take(limit)
            .ToListAsync();

        var specializations = doctors
            .Select(d => d.Specialization)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        return Ok(new PublicDoctorListResponse
        {
            TotalCount = doctors.Count,
            Doctors = doctors.Select(MapToPublicDto).ToList(),
            AvailableSpecializations = specializations
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PublicDoctorDto>> GetDoctor(int id)
    {
        var d = await _context.DoctorProfiles
            .Include(x => x.User)
            .Include(x => x.Department)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (d == null) return NotFound(new { message = "Doctor not found." });
        return Ok(MapToPublicDto(d));
    }

    private static PublicDoctorDto MapToPublicDto(DoctorProfile d) => new()
    {
        Id = d.Id,
        FullName = $"Dr. {d.User.FirstName} {d.User.LastName}".Trim(),
        Title = d.User.Designation ?? string.Empty,
        Specialization = d.Specialization,
        DepartmentId = d.DepartmentId,
        DepartmentName = d.Department?.Name,
        Bio = d.Bio,
        YearsOfExperience = d.YearsOfExperience,
        Languages = ParseJsonArray(d.LanguagesJson),
        Qualifications = ParseJsonArray(d.QualificationsJson),
        ConsultationFee = d.ConsultationFee,
        FeeCurrency = "BDT",
        ConsultationDurationMinutes = d.ConsultationDurationMinutes,
        IsAcceptingPatients = d.IsAcceptingPatients,
        PhotoUrl = d.PhotoUrl,
        TenantId = d.TenantId,
        HospitalName = d.Tenant?.Name ?? string.Empty,
        HospitalLogoUrl = d.Tenant?.LogoUrl,
        HospitalSubdomain = d.Tenant?.Subdomain
    };

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            // tolerate non-JSON (e.g. a comma-separated string)
            return json.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }
}
