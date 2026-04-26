using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Patients;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PatientService> _logger;

    public PatientService(ApplicationDbContext context, ILogger<PatientService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PatientListResponse> GetPatientsAsync(int tenantId, PatientQueryParams queryParams)
    {
        var query = _context.Patients
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (queryParams.IsActive.HasValue)
            query = query.Where(p => p.IsActive == queryParams.IsActive.Value);

        if (queryParams.Gender.HasValue)
            query = query.Where(p => p.Gender == queryParams.Gender.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.BloodGroup))
            query = query.Where(p => p.BloodGroup == queryParams.BloodGroup);

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

        query = queryParams.SortBy?.ToLower() switch
        {
            "lastname" => queryParams.SortOrder == "asc"
                ? query.OrderBy(p => p.LastName)
                : query.OrderByDescending(p => p.LastName),
            "patientnumber" => queryParams.SortOrder == "asc"
                ? query.OrderBy(p => p.PatientNumber)
                : query.OrderByDescending(p => p.PatientNumber),
            _ => queryParams.SortOrder == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt)
        };

        var pageSize = Math.Min(queryParams.PageSize, 100);
        var pageNumber = Math.Max(queryParams.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var patients = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PatientListResponse
        {
            Patients = patients.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    public async Task<PatientDto?> GetPatientByIdAsync(int tenantId, int patientId)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId);

        return patient is null ? null : MapToDto(patient);
    }

    public async Task<PatientDto> CreatePatientAsync(int tenantId, int createdByUserId, CreatePatientRequest request)
    {
        var patientNumber = await GeneratePatientNumberAsync(tenantId);

        var patient = new Patient
        {
            TenantId = tenantId,
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
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Patient created: {PatientNumber} in Tenant {TenantId} by User {UserId}",
            patient.PatientNumber, tenantId, createdByUserId);

        return MapToDto(patient);
    }

    public async Task<PatientDto> UpdatePatientAsync(int tenantId, int patientId, UpdatePatientRequest request)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Patient {patientId} not found.");

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

        _logger.LogInformation("Patient updated: {PatientId} in Tenant {TenantId}", patientId, tenantId);

        return MapToDto(patient);
    }

    public async Task DeactivatePatientAsync(int tenantId, int patientId)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Patient {patientId} not found.");

        patient.IsActive = false;
        patient.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Patient deactivated: {PatientId} in Tenant {TenantId}", patientId, tenantId);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<string> GeneratePatientNumberAsync(int tenantId)
    {
        var maxNumber = await _context.Patients
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.PatientNumber)
            .Select(p => p.PatientNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (maxNumber != null && maxNumber.StartsWith("P-") &&
            int.TryParse(maxNumber[2..], out var current))
        {
            nextSeq = current + 1;
        }

        return $"P-{nextSeq:D6}";
    }

    internal static PatientDto MapToDto(Patient p) => new()
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
