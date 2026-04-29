using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.AI;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<TenantSettingsController> _logger;

    public TenantSettingsController(
        ApplicationDbContext context,
        IStorageService storageService,
        ILogger<TenantSettingsController> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Get current tenant's settings
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Master")]
    public async Task<ActionResult<TenantSettingsDto>> GetSettings()
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        return Ok(new TenantSettingsDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Address = tenant.Address,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone,
            LogoUrl = tenant.LogoUrl,
            Description = tenant.Description,
            OperatingHours = tenant.OperatingHours,
            TimeZone = tenant.TimeZone,
            BusinessRegistrationNumber = tenant.BusinessRegistrationNumber,
            SubscriptionPlan = tenant.SubscriptionPlan,
            UserLicenses = tenant.UserLicenses,
            ModuleOpdEnabled = tenant.ModuleOpdEnabled,
            ModuleIpdEnabled = tenant.ModuleIpdEnabled,
            ModuleEmergencyEnabled = tenant.ModuleEmergencyEnabled,
            ModulePharmacyEnabled = tenant.ModulePharmacyEnabled,
            ModuleLaboratoryEnabled = tenant.ModuleLaboratoryEnabled,
            ModuleRadiologyEnabled = tenant.ModuleRadiologyEnabled,
            ModuleBillingEnabled = tenant.ModuleBillingEnabled,
            ModuleInventoryEnabled = tenant.ModuleInventoryEnabled,
            ModuleHrPayrollEnabled = tenant.ModuleHrPayrollEnabled,
            ModuleAppointmentEnabled = tenant.ModuleAppointmentEnabled,
            ModuleTelemedicineEnabled = tenant.ModuleTelemedicineEnabled,
            ModuleAnalyticsEnabled = tenant.ModuleAnalyticsEnabled
        });
    }

    /// <summary>
    /// Update tenant settings
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Admin,Master")]
    public async Task<ActionResult<TenantSettingsDto>> UpdateSettings([FromBody] UpdateTenantSettingsRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        // Update fields
        if (request.Name != null) tenant.Name = request.Name;
        if (request.Address != null) tenant.Address = request.Address;
        if (request.ContactEmail != null) tenant.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) tenant.ContactPhone = request.ContactPhone;
        if (request.Description != null) tenant.Description = request.Description;
        if (request.OperatingHours != null) tenant.OperatingHours = request.OperatingHours;
        if (request.TimeZone != null) tenant.TimeZone = request.TimeZone;
        if (request.BusinessRegistrationNumber != null) tenant.BusinessRegistrationNumber = request.BusinessRegistrationNumber;

        tenant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} settings updated by user {UserId}", tenantId, GetCurrentUserId());

        return Ok(new TenantSettingsDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Address = tenant.Address,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone,
            LogoUrl = tenant.LogoUrl,
            Description = tenant.Description,
            OperatingHours = tenant.OperatingHours,
            TimeZone = tenant.TimeZone,
            BusinessRegistrationNumber = tenant.BusinessRegistrationNumber
        });
    }

    /// <summary>
    /// Upload tenant logo
    /// </summary>
    [HttpPost("logo")]
    [Authorize(Roles = "Admin,Master")]
    public async Task<ActionResult<LogoUploadResponse>> UploadLogo(IFormFile file)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Invalid file type. Allowed: JPG, PNG, GIF, WebP" });

        // Max 2MB
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { message = "File too large. Max 2MB allowed" });

        try
        {
            // Upload to storage
            var fileName = $"tenant-{tenantId}/logo.{file.ContentType.Split('/')[1]}";
            var uploadResult = await _storageService.UploadAsync(file.OpenReadStream(), fileName, file.ContentType);
            if (!uploadResult.Success || string.IsNullOrEmpty(uploadResult.StorageFileId))
                return StatusCode(500, new { message = "Storage upload failed" });
            var storageKey = uploadResult.StorageFileId;

            // Update tenant with logo URL
            var tenant = await _context.Tenants.FindAsync(tenantId.Value);
            if (tenant == null) return NotFound();

            tenant.LogoUrl = storageKey;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate download URL
            var logoUrl = await _storageService.GenerateDownloadUrlAsync(storageKey, 1440);

            _logger.LogInformation("Tenant {TenantId} logo updated", tenantId);

            return Ok(new LogoUploadResponse
            {
                LogoUrl = logoUrl,
                StorageKey = storageKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Error uploading logo" });
        }
    }

    /// <summary>
    /// Get logo download URL
    /// </summary>
    [HttpGet("logo")]
    [AllowAnonymous]
    public async Task<ActionResult<LogoUploadResponse>> GetLogo()
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null && Request.Query.ContainsKey("tenantId"))
        {
            if (int.TryParse(Request.Query["tenantId"], out var tid))
                tenantId = tid;
        }

        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID required" });

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        if (string.IsNullOrEmpty(tenant.LogoUrl))
            return NotFound(new { message = "No logo uploaded" });

        var logoUrl = await _storageService.GenerateDownloadUrlAsync(tenant.LogoUrl, 1440);

        return Ok(new LogoUploadResponse
        {
            LogoUrl = logoUrl,
            StorageKey = tenant.LogoUrl
        });
    }

    private int? GetCurrentTenantId()
    {
        var claim = User.FindFirst("TenantId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("nameidentifier")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
