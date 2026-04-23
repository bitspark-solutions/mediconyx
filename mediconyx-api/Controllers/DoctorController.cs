using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Interfaces;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DoctorController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<DoctorController> _logger;

    public DoctorController(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ILogger<DoctorController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// List all doctors in the tenant
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<DoctorListResponse>> GetDoctors(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? specialization = null,
        [FromQuery] bool includeInactive = false)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.DoctorProfiles
            .Include(d => d.User)
            .Include(d => d.Department)
            .Where(d => d.TenantId == tenantId);

        if (!includeInactive)
            query = query.Where(d => d.IsActive);

        if (!string.IsNullOrEmpty(specialization))
            query = query.Where(d => d.Specialization == specialization);

        var totalCount = await query.CountAsync();

        var doctors = await query
            .OrderBy(d => d.User.LastName)
            .ThenBy(d => d.User.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new DoctorListResponse
        {
            Doctors = doctors.Select(MapToDto).ToList(),
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get a single doctor by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist")]
    public async Task<ActionResult<DoctorProfileDto>> GetDoctor(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var doctor = await _context.DoctorProfiles
            .Include(d => d.User)
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);

        if (doctor == null) return NotFound();

        return Ok(MapToDto(doctor));
    }

    /// <summary>
    /// Create a new doctor (user + profile)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DoctorProfileDto>> CreateDoctor([FromBody] CreateDoctorRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        // Validate specialization
        if (!MedicalSpecializations.All.Contains(request.Specialization))
            return BadRequest(new { message = $"Invalid specialization. Allowed: {string.Join(", ", MedicalSpecializations.All)}" });

        // Check if email already exists in tenant
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == request.Email.ToLower());
        if (existingUser != null)
            return BadRequest(new { message = "Email already registered in this hospital" });

        // Create user
        var user = new User
        {
            TenantId = tenantId.Value,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = UserRole.Doctor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create doctor profile
        var doctorProfile = new DoctorProfile
        {
            UserId = user.Id,
            TenantId = tenantId.Value,
            MedicalLicenseNumber = request.MedicalLicenseNumber,
            Specialization = request.Specialization,
            DepartmentId = request.DepartmentId,
            ConsultationDurationMinutes = request.ConsultationDurationMinutes,
            ConsultationFee = request.ConsultationFee,
            IsAcceptingPatients = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.DoctorProfiles.Add(doctorProfile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Doctor {DoctorId} created for tenant {TenantId}", doctorProfile.Id, tenantId);

        // Reload with includes
        await _context.Entry(doctorProfile).Reference(d => d.User).LoadAsync();
        if (doctorProfile.DepartmentId.HasValue)
            await _context.Entry(doctorProfile).Reference(d => d.Department).LoadAsync();

        return CreatedAtAction(nameof(GetDoctor), new { id = doctorProfile.Id }, MapToDto(doctorProfile));
    }

    /// <summary>
    /// Update a doctor's profile
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DoctorProfileDto>> UpdateDoctor(int id, [FromBody] UpdateDoctorRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var doctor = await _context.DoctorProfiles
            .Include(d => d.User)
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);

        if (doctor == null) return NotFound();

        // Validate specialization if provided
        if (request.Specialization != null && !MedicalSpecializations.All.Contains(request.Specialization))
            return BadRequest(new { message = $"Invalid specialization. Allowed: {string.Join(", ", MedicalSpecializations.All)}" });

        // Update user fields
        if (request.FirstName != null) doctor.User.FirstName = request.FirstName;
        if (request.LastName != null) doctor.User.LastName = request.LastName;
        if (request.Phone != null) doctor.User.Phone = request.Phone;

        // Update profile fields
        if (request.MedicalLicenseNumber != null) doctor.MedicalLicenseNumber = request.MedicalLicenseNumber;
        if (request.Specialization != null) doctor.Specialization = request.Specialization;
        if (request.DepartmentId.HasValue) doctor.DepartmentId = request.DepartmentId;
        if (request.ConsultationDurationMinutes.HasValue) doctor.ConsultationDurationMinutes = request.ConsultationDurationMinutes.Value;
        if (request.ConsultationFee.HasValue) doctor.ConsultationFee = request.ConsultationFee;
        if (request.IsAcceptingPatients.HasValue) doctor.IsAcceptingPatients = request.IsAcceptingPatients.Value;
        if (request.IsActive.HasValue) doctor.IsActive = request.IsActive.Value;

        doctor.UpdatedAt = DateTime.UtcNow;
        doctor.User.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Doctor {DoctorId} updated", id);

        return Ok(MapToDto(doctor));
    }

    /// <summary>
    /// Deactivate a doctor (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeactivateDoctor(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var doctor = await _context.DoctorProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);

        if (doctor == null) return NotFound();

        doctor.IsActive = false;
        doctor.IsAcceptingPatients = false;
        doctor.User.IsActive = false;
        doctor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Doctor {DoctorId} deactivated", id);

        return NoContent();
    }

    /// <summary>
    /// Get doctors by specialization
    /// </summary>
    [HttpGet("by-specialization/{specialization}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    public async Task<ActionResult<List<DoctorProfileDto>>> GetBySpecialization(string specialization)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var doctors = await _context.DoctorProfiles
            .Include(d => d.User)
            .Include(d => d.Department)
            .Where(d => d.TenantId == tenantId
                && d.Specialization == specialization
                && d.IsActive
                && d.IsAcceptingPatients)
            .OrderBy(d => d.User.LastName)
            .ToListAsync();

        return Ok(doctors.Select(MapToDto).ToList());
    }

    private int? GetCurrentTenantId()
    {
        var claim = User.FindFirst("TenantId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static DoctorProfileDto MapToDto(DoctorProfile doctor)
    {
        return new DoctorProfileDto
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            FirstName = doctor.User?.FirstName ?? "",
            LastName = doctor.User?.LastName ?? "",
            Email = doctor.User?.Email ?? "",
            Phone = doctor.User?.Phone,
            MedicalLicenseNumber = doctor.MedicalLicenseNumber,
            Specialization = doctor.Specialization,
            DepartmentId = doctor.DepartmentId,
            DepartmentName = doctor.Department?.Name,
            ConsultationDurationMinutes = doctor.ConsultationDurationMinutes,
            ConsultationFee = doctor.ConsultationFee,
            IsAcceptingPatients = doctor.IsAcceptingPatients,
            IsActive = doctor.IsActive,
            CreatedAt = doctor.CreatedAt
        };
    }
}