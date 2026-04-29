using System.Security.Claims;
using System.Text.Json;
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
public class FileAttachmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<FileAttachmentController> _logger;

    public FileAttachmentController(
        ApplicationDbContext context,
        IStorageService storageService,
        ILogger<FileAttachmentController> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    public async Task<ActionResult<FileAttachmentDto>> Upload(
        [FromForm] int patientId,
        [FromForm] int? appointmentId,
        [FromForm] FileAttachmentCategory category,
        [FromForm] string? description,
        [FromForm] IFormFile file)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        if (tenantId == null || userId == null)
            return Unauthorized();

        // Validate patient belongs to tenant
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId);
        if (patient == null)
            return BadRequest(new { message = "Patient not found." });

        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        // File size validation (50MB max for lab reports, 500MB for DICOM)
        var maxSize = category == FileAttachmentCategory.Imaging ? 500 * 1024 * 1024 : 50 * 1024 * 1024;
        if (file.Length > maxSize)
            return BadRequest(new { message = $"File size exceeds maximum allowed ({maxSize / 1024 / 1024}MB)." });

        // Content type validation
        var allowedTypes = new[]
        {
            "application/pdf",
            "image/jpeg",
            "image/png",
            "image/gif",
            "application/dicom",
            "application/octet-stream"
        };
        if (!allowedTypes.Any(t => file.ContentType.Contains(t, StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "File type not allowed. Allowed: PDF, JPEG, PNG, GIF, DICOM." });

        // Upload to storage service
        using var stream = file.OpenReadStream();
        var uploadResult = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);
        if (!uploadResult.Success || string.IsNullOrEmpty(uploadResult.StorageFileId))
            return StatusCode(500, new { message = uploadResult.ErrorMessage ?? "Failed to upload file." });

        // Create attachment record
        var attachment = new FileAttachment
        {
            TenantId = tenantId.Value,
            PatientId = patientId,
            AppointmentId = appointmentId,
            UploadedByUserId = userId.Value,
            StorageFileId = uploadResult.StorageFileId,
            FileName = file.FileName,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Category = category,
            Description = description,
            IsActive = true,
            UploadedAt = DateTime.UtcNow
        };

        _context.FileAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "File uploaded: {FileId} for Patient {PatientId} by User {UserId}, Size: {Size}",
            attachment.Id, patientId, userId.Value, file.Length);

        return CreatedAtAction(nameof(GetById), new { id = attachment.Id }, await MapToDto(attachment));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    public async Task<ActionResult<FileAttachmentDto>> GetById(int id)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (tenantId == null || userId == null)
            return Unauthorized();

        var attachment = await _context.FileAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId && a.IsActive);

        if (attachment == null)
            return NotFound(new { message = "File attachment not found." });

        // Patients can only access their own files
        if (userRole == "Patient" && attachment.PatientId != userId)
            return Forbid();

        return await MapToDto(attachment);
    }

    [HttpGet("{id}/download")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    public async Task<IActionResult> Download(int id)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (tenantId == null || userId == null)
            return Unauthorized();

        var attachment = await _context.FileAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId && a.IsActive);

        if (attachment == null)
            return NotFound(new { message = "File attachment not found." });

        // Patients can only access their own files
        if (userRole == "Patient" && attachment.PatientId != userId)
            return Forbid();

        var stream = await _storageService.DownloadAsync(attachment.StorageFileId);
        if (stream == null)
            return NotFound(new { message = "File not found in storage." });

        return File(stream, attachment.ContentType, attachment.OriginalFileName);
    }

    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    public async Task<ActionResult<FileAttachmentListResponse>> GetByPatient(
        int patientId,
        [FromQuery] int? appointmentId,
        [FromQuery] FileAttachmentCategory? category,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "UploadedAt",
        [FromQuery] string sortOrder = "desc")
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (tenantId == null || userId == null)
            return Unauthorized();

        // Validate patient belongs to tenant
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId);
        if (patient == null)
            return NotFound(new { message = "Patient not found." });

        // Patients can only access their own files
        if (userRole == "Patient" && patientId != userId)
            return Forbid();

        var query = _context.FileAttachments
            .Where(a => a.TenantId == tenantId.Value && a.PatientId == patientId && a.IsActive);

        if (appointmentId.HasValue)
            query = query.Where(a => a.AppointmentId == appointmentId.Value);

        if (category.HasValue)
            query = query.Where(a => a.Category == category.Value);

        // Sorting
        query = sortBy.ToLower() switch
        {
            "filename" => sortOrder.ToLower() == "asc" ? query.OrderBy(a => a.FileName) : query.OrderByDescending(a => a.FileName),
            "category" => sortOrder.ToLower() == "asc" ? query.OrderBy(a => a.Category) : query.OrderByDescending(a => a.Category),
            "filesize" => sortOrder.ToLower() == "asc" ? query.OrderBy(a => a.FileSize) : query.OrderByDescending(a => a.FileSize),
            _ => sortOrder.ToLower() == "asc" ? query.OrderBy(a => a.UploadedAt) : query.OrderByDescending(a => a.UploadedAt)
        };

        var pageSizeClamped = Math.Clamp(pageSize, 1, 100);
        var pageNumberClamped = Math.Max(1, pageNumber);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSizeClamped);

        var attachments = await query
            .Skip((pageNumberClamped - 1) * pageSizeClamped)
            .Take(pageSizeClamped)
            .ToListAsync();

        return new FileAttachmentListResponse
        {
            Attachments = (await Task.WhenAll(attachments.Select(MapToDto))).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumberClamped,
            PageSize = pageSizeClamped,
            TotalPages = totalPages
        };
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null)
            return Unauthorized();

        var attachment = await _context.FileAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId && a.IsActive);

        if (attachment == null)
            return NotFound(new { message = "File attachment not found." });

        // Soft delete
        attachment.IsActive = false;
        await _context.SaveChangesAsync();

        // Also delete from storage
        await _storageService.DeleteAsync(attachment.StorageFileId);

        _logger.LogInformation("File attachment deleted: {FileId} by Admin {AdminId}", id, GetCurrentUserId());

        return NoContent();
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

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
    }

    private async Task<FileAttachmentDto> MapToDto(FileAttachment a)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == a.PatientId);
        var uploader = await _context.Users.FirstOrDefaultAsync(u => u.Id == a.UploadedByUserId);

        return new FileAttachmentDto
        {
            Id = a.Id,
            StorageFileId = a.StorageFileId,
            PatientId = a.PatientId,
            PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "",
            AppointmentId = a.AppointmentId,
            FileName = a.FileName,
            OriginalFileName = a.OriginalFileName,
            ContentType = a.ContentType,
            FileSize = a.FileSize,
            Category = a.Category.ToString(),
            Description = a.Description,
            IsActive = a.IsActive,
            UploadedAt = a.UploadedAt,
            UploadedByUserName = uploader != null ? $"{uploader.FirstName} {uploader.LastName}" : ""
        };
    }
}

