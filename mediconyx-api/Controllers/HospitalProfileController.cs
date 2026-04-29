using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/sales/requests/{requestId:int}/hospital-profile")]
[Authorize(Roles = "Sales,Admin,Master")]
public class HospitalProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HospitalProfileController> _logger;

    public HospitalProfileController(
        ApplicationDbContext context,
        ILogger<HospitalProfileController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<HospitalProfileResponseDto>> Get(int requestId)
    {
        var profile = await _context.HospitalProfiles
            .Where(p => p.SalesRequestId == requestId)
            .FirstOrDefaultAsync();

        if (profile == null)
        {
            return NotFound(new { message = "Hospital profile not found for this request." });
        }

        return Ok(MapToResponseDto(profile));
    }

    [HttpPost]
    public async Task<ActionResult<HospitalProfileResponseDto>> CreateOrUpdate(
        int requestId,
        [FromBody] HospitalProfileCreateDto dto)
    {
        // Validate SalesRequest exists
        var salesRequest = await _context.SalesRequests
            .Include(r => r.HospitalProfile)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (salesRequest == null)
        {
            return NotFound(new { message = "Sales request not found." });
        }

        // Check for duplicate license number (excluding current profile)
        var existingLicense = await _context.HospitalProfiles
            .Where(p => p.LicenseNumber == dto.LicenseNumber && p.SalesRequestId != requestId)
            .AnyAsync();

        if (existingLicense)
        {
            return BadRequest(new { message = "License number already exists in the system." });
        }

        // Check for duplicate tax ID (excluding current profile)
        var existingTaxId = await _context.HospitalProfiles
            .Where(p => p.TaxId == dto.TaxId && p.SalesRequestId != requestId)
            .AnyAsync();

        if (existingTaxId)
        {
            return BadRequest(new { message = "Tax ID already exists in the system." });
        }

        HospitalProfile profile;
        bool isNewProfile = false;

        if (salesRequest.HospitalProfile == null)
        {
            // Create new profile
            profile = new HospitalProfile
            {
                SalesRequestId = requestId
            };
            isNewProfile = true;
        }
        else
        {
            // Update existing profile
            profile = salesRequest.HospitalProfile;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        // Map DTO to entity
        MapDtoToEntity(dto, profile);

        if (isNewProfile)
        {
            _context.HospitalProfiles.Add(profile);
            
            // If request is Qualified, transition to InProgress
            if (salesRequest.Status == SalesRequestStatus.Qualified)
            {
                salesRequest.Status = SalesRequestStatus.InProgress;
                salesRequest.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Create timeline event
        var timelineEvent = new OnboardingTimelineEvent
        {
            SalesRequestId = requestId,
            Type = "ProfileSubmitted",
            Description = isNewProfile 
                ? "Hospital profile created" 
                : "Hospital profile updated",
            HappenedAt = DateTime.UtcNow
        };
        _context.OnboardingTimelineEvents.Add(timelineEvent);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Hospital profile {Action} for SalesRequest {RequestId}",
            isNewProfile ? "created" : "updated",
            requestId);

        var response = MapToResponseDto(profile);
        
        return isNewProfile 
            ? CreatedAtAction(nameof(Get), new { requestId }, response)
            : Ok(response);
    }

    private void MapDtoToEntity(HospitalProfileCreateDto dto, HospitalProfile entity)
    {
        entity.HospitalLegalName = dto.HospitalLegalName;
        entity.LicenseNumber = dto.LicenseNumber;
        entity.TaxId = dto.TaxId;
        entity.HospitalType = dto.HospitalType;
        entity.BedCapacity = dto.BedCapacity;
        entity.AddressLine1 = dto.AddressLine1;
        entity.AddressLine2 = dto.AddressLine2;
        entity.City = dto.City;
        entity.StateProvince = dto.StateProvince;
        entity.PostalCode = dto.PostalCode;
        entity.Country = dto.Country;
        entity.PrimaryContactName = dto.PrimaryContactName;
        entity.PrimaryContactEmail = dto.PrimaryContactEmail;
        entity.PrimaryContactPhone = dto.PrimaryContactPhone;
        entity.SecondaryContactName = dto.SecondaryContactName;
        entity.SecondaryContactEmail = dto.SecondaryContactEmail;
        entity.SecondaryContactPhone = dto.SecondaryContactPhone;
        entity.WebsiteUrl = dto.WebsiteUrl;
        entity.EstablishedYear = dto.EstablishedYear;
    }

    private HospitalProfileResponseDto MapToResponseDto(HospitalProfile entity)
    {
        return new HospitalProfileResponseDto
        {
            Id = entity.Id,
            SalesRequestId = entity.SalesRequestId,
            HospitalLegalName = entity.HospitalLegalName,
            LicenseNumber = entity.LicenseNumber,
            TaxId = entity.TaxId,
            HospitalType = entity.HospitalType,
            BedCapacity = entity.BedCapacity,
            AddressLine1 = entity.AddressLine1,
            AddressLine2 = entity.AddressLine2,
            City = entity.City,
            StateProvince = entity.StateProvince,
            PostalCode = entity.PostalCode,
            Country = entity.Country,
            PrimaryContactName = entity.PrimaryContactName,
            PrimaryContactEmail = entity.PrimaryContactEmail,
            PrimaryContactPhone = entity.PrimaryContactPhone,
            SecondaryContactName = entity.SecondaryContactName,
            SecondaryContactEmail = entity.SecondaryContactEmail,
            SecondaryContactPhone = entity.SecondaryContactPhone,
            WebsiteUrl = entity.WebsiteUrl,
            EstablishedYear = entity.EstablishedYear,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
