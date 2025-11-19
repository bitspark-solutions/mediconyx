using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services;
using System.Security.Cryptography;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/sales/requests")]
public class SalesRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SalesRequestsController> _logger;
    private readonly ICaptchaService _captchaService;
    private readonly IFileValidationService _fileValidationService;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;

    public SalesRequestsController(
        ApplicationDbContext context, 
        ILogger<SalesRequestsController> logger,
        ICaptchaService captchaService,
        IFileValidationService fileValidationService,
        IMemoryCache cache,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _captchaService = captchaService;
        _fileValidationService = fileValidationService;
        _cache = cache;
        _emailService = emailService;
    }

    [HttpPost("public")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CreatePublic(PublicSalesRequestCreate request)
    {
        // Validate captcha token
        if (string.IsNullOrWhiteSpace(request.CaptchaToken))
        {
            return BadRequest(new { message = "Captcha is required." });
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var captchaValid = await _captchaService.VerifyTokenAsync(request.CaptchaToken, remoteIp);
        
        if (!captchaValid)
        {
            _logger.LogWarning("Invalid captcha attempt from IP: {IP}", remoteIp);
            return BadRequest(new { message = "Invalid captcha. Please try again." });
        }

        // Input validation and sanitization
        if (string.IsNullOrWhiteSpace(request.HospitalName) || request.HospitalName.Length > 300)
        {
            return BadRequest(new { message = "Invalid hospital name." });
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            return BadRequest(new { message = "Invalid email address." });
        }

        var sr = new SalesRequest
        {
            HospitalName = SanitizeInput(request.HospitalName),
            ContactPerson = SanitizeInput(request.ContactPerson),
            Email = request.Email.ToLowerInvariant().Trim(),
            Phone = SanitizeInput(request.Phone),
            Region = SanitizeInput(request.Region),
            Notes = SanitizeInput(request.Notes),
            Status = SalesRequestStatus.Pending,
            SourceIp = remoteIp,
            UserAgent = Request.Headers.UserAgent.ToString(),
            SubscriptionPlan = SanitizeInput(request.SubscriptionPlan),
            BillingCycle = request.BillingCycle,
            Currency = request.Currency,
            QuotedPrice = request.QuotedPrice,
            Seats = request.Seats,
            CreatedAt = DateTime.UtcNow
        };

        _context.SalesRequests.Add(sr);
        await _context.SaveChangesAsync();

        // Send email notification (fire and forget)
        _ = _emailService.SendNewRequestNotificationAsync(sr.Id, sr.HospitalName, sr.Email);

        InvalidateMetricsCache();
        _logger.LogInformation("Public sales request created {Id} from IP {IP}", sr.Id, remoteIp);
        return Created($"/api/sales/requests/{sr.Id}", new { id = sr.Id, status = sr.Status.ToString().ToLower() });
    }

    [HttpGet]
    [Authorize(Roles = "Sales,Master")]
    public async Task<ActionResult<object>> List([FromQuery] SalesRequestQuery q)
    {
        var query = _context.SalesRequests.AsQueryable();
        if (q.Status.HasValue) query = query.Where(r => r.Status == q.Status);
        if (q.OwnerId.HasValue) query = query.Where(r => r.OwnerSalesUserId == q.OwnerId);
        if (!string.IsNullOrWhiteSpace(q.Region)) query = query.Where(r => r.Region == q.Region);
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.ToLower();
            query = query.Where(r => r.HospitalName.ToLower().Contains(s) || r.Email.ToLower().Contains(s));
        }
        var total = await query.CountAsync();
        var pageSize = Math.Min(q.PageSize, 100);
        var page = Math.Max(q.Page, 1);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.HospitalName,
                r.ContactPerson,
                r.Email,
                r.Region,
                r.Status,
                r.OwnerSalesUserId,
                r.CreatedAt,
                r.QuotedPrice,
                r.Currency,
                r.PaymentStatus
            })
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<ActionResult<object>> Detail(int id)
    {
        var r = await _context.SalesRequests
            .Include(x => x.Attachments)
            .Include(x => x.NotesList)
            .Include(x => x.Timeline)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        return Ok(r);
    }

    [HttpPost]
    [Authorize(Roles = "Sales,Master")]
    public async Task<ActionResult<object>> CreateInternal(PublicSalesRequestCreate request)
    {
        var sr = new SalesRequest
        {
            HospitalName = request.HospitalName,
            ContactPerson = request.ContactPerson,
            Email = request.Email.ToLowerInvariant(),
            Phone = request.Phone,
            Region = request.Region,
            Notes = request.Notes,
            Status = SalesRequestStatus.Pending,
            SubscriptionPlan = request.SubscriptionPlan,
            BillingCycle = request.BillingCycle,
            Currency = request.Currency,
            QuotedPrice = request.QuotedPrice,
            Seats = request.Seats,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(sr);
        await _context.SaveChangesAsync();
        InvalidateMetricsCache();
        return CreatedAtAction(nameof(Detail), new { id = sr.Id }, sr);
    }

    [HttpPost("{id:int}/claim")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<IActionResult> Claim(int id)
    {
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound();
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        r.OwnerSalesUserId = userId;
        r.ClaimedAt = DateTime.UtcNow;
        r.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        InvalidateMetricsCache();
        return NoContent();
    }

    [HttpPost("{id:int}/unassign")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<IActionResult> Unassign(int id)
    {
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound();
        r.OwnerSalesUserId = null;
        r.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        InvalidateMetricsCache();
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<IActionResult> UpdateStatus(int id, SalesStatusUpdate update)
    {
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound();
        var old = r.Status;
        r.Status = update.Status;
        if (update.Status == SalesRequestStatus.Active) r.ActivatedAt = DateTime.UtcNow;
        if (update.Status == SalesRequestStatus.Deactivated) r.DeactivatedAt = DateTime.UtcNow;
        r.UpdatedAt = DateTime.UtcNow;
        
        // Send email notification
        _ = _emailService.SendStatusChangeNotificationAsync(r.Id, r.HospitalName, r.Email, old.ToString(), r.Status.ToString());
        
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(SalesRequest),
            EntityId = r.Id.ToString(),
            Action = "status_update",
            ActorUserId = GetCurrentUserId(),
            OldValues = old.ToString(),
            NewValues = r.Status.ToString(),
            Reason = update.Reason,
            CreatedAt = DateTime.UtcNow
        });
        _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
        {
            SalesRequestId = r.Id,
            Type = "status",
            Description = $"{old} -> {r.Status}",
            HappenedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:int}/pricing")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<IActionResult> UpdatePricing(int id, PricingUpdate update)
    {
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound();
        r.SubscriptionPlan = update.SubscriptionPlan ?? r.SubscriptionPlan;
        r.BillingCycle = update.BillingCycle ?? r.BillingCycle;
        r.Currency = update.Currency ?? r.Currency;
        r.QuotedPrice = update.QuotedPrice ?? r.QuotedPrice;
        r.Seats = update.Seats ?? r.Seats;
        r.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        InvalidateMetricsCache();
        return NoContent();
    }

    [HttpPatch("{id:int}/payment-status")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<IActionResult> UpdatePaymentStatus(int id, PaymentStatusUpdate update)
    {
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound();
        r.PaymentStatus = update.PaymentStatus;
        r.PaidAt = update.PaymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : r.PaidAt;
        r.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        InvalidateMetricsCache();
        return NoContent();
    }

    [HttpPatch("{id:int}/qualify")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<ActionResult<SalesRequest>> Qualify(int id)
    {
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound(new { message = "Sales request not found." });
        
        if (r.Status != SalesRequestStatus.Pending)
        {
            return BadRequest(new { message = "Only pending requests can be qualified." });
        }

        r.Status = SalesRequestStatus.Qualified;
        r.UpdatedAt = DateTime.UtcNow;

        var userId = GetCurrentUserId();
        var userName = User.Identity?.Name ?? "Unknown";

        _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
        {
            SalesRequestId = r.Id,
            Type = "StatusChanged",
            Description = $"Request qualified by {userName}",
            HappenedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        InvalidateMetricsCache();

        // Send qualification email (fire and forget)
        _ = _emailService.SendStatusChangeNotificationAsync(r.Id, r.HospitalName, r.Email, "Pending", "Qualified");

        _logger.LogInformation("SalesRequest {Id} qualified by user {UserId}", id, userId);

        return Ok(r);
    }

    [HttpPatch("{id:int}/reject")]
    [Authorize(Roles = "Sales,Admin,Master")]
    public async Task<ActionResult<SalesRequest>> Reject(int id, [FromBody] StatusActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Reason is required for rejection." });
        }

        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound(new { message = "Sales request not found." });

        if (r.Status == SalesRequestStatus.Rejected)
        {
            return BadRequest(new { message = "Request is already rejected." });
        }

        var oldStatus = r.Status;
        r.Status = SalesRequestStatus.Rejected;
        r.DeactivatedAt = DateTime.UtcNow;
        r.UpdatedAt = DateTime.UtcNow;

        var userName = User.Identity?.Name ?? "Unknown";

        _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
        {
            SalesRequestId = r.Id,
            Type = "Rejected",
            Description = $"Request rejected by {userName}: {request.Reason}",
            HappenedAt = DateTime.UtcNow
        });

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(SalesRequest),
            EntityId = r.Id.ToString(),
            Action = "reject",
            ActorUserId = GetCurrentUserId(),
            OldValues = oldStatus.ToString(),
            NewValues = SalesRequestStatus.Rejected.ToString(),
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        InvalidateMetricsCache();

        // Send rejection email (fire and forget)
        _ = _emailService.SendStatusChangeNotificationAsync(r.Id, r.HospitalName, r.Email, oldStatus.ToString(), "Rejected");

        _logger.LogInformation("SalesRequest {Id} rejected: {Reason}", id, request.Reason);

        return Ok(r);
    }

    [HttpPatch("{id:int}/suspend")]
    [Authorize(Roles = "Admin,Master")]
    public async Task<ActionResult<SalesRequest>> Suspend(int id, [FromBody] StatusActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Reason is required for suspension." });
        }

        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound(new { message = "Sales request not found." });

        if (r.Status != SalesRequestStatus.Active)
        {
            return BadRequest(new { message = "Only active requests can be suspended." });
        }

        r.Status = SalesRequestStatus.Suspended;
        r.DeactivatedAt = DateTime.UtcNow;
        r.UpdatedAt = DateTime.UtcNow;

        var userName = User.Identity?.Name ?? "Unknown";

        _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
        {
            SalesRequestId = r.Id,
            Type = "StatusChanged",
            Description = $"Request suspended by {userName}: {request.Reason}",
            HappenedAt = DateTime.UtcNow
        });

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(SalesRequest),
            EntityId = r.Id.ToString(),
            Action = "suspend",
            ActorUserId = GetCurrentUserId(),
            OldValues = SalesRequestStatus.Active.ToString(),
            NewValues = SalesRequestStatus.Suspended.ToString(),
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        InvalidateMetricsCache();

        _logger.LogInformation("SalesRequest {Id} suspended: {Reason}", id, request.Reason);

        return Ok(r);
    }

    [HttpPatch("{id:int}/reactivate")]
    [Authorize(Roles = "Admin,Master")]
    public async Task<ActionResult<SalesRequest>> Reactivate(int id)
    {
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound(new { message = "Sales request not found." });

        if (r.Status != SalesRequestStatus.Suspended)
        {
            return BadRequest(new { message = "Only suspended requests can be reactivated." });
        }

        r.Status = SalesRequestStatus.Active;
        r.DeactivatedAt = null;
        r.UpdatedAt = DateTime.UtcNow;

        var userName = User.Identity?.Name ?? "Unknown";

        _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
        {
            SalesRequestId = r.Id,
            Type = "StatusChanged",
            Description = $"Request reactivated by {userName}",
            HappenedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        InvalidateMetricsCache();

        _logger.LogInformation("SalesRequest {Id} reactivated", id);

        return Ok(r);
    }

    [HttpPost("{id:int}/notes")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<IActionResult> AddNote(int id, [FromBody] string note)
    {
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound();
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        _context.SalesRequestNotes.Add(new SalesRequestNote
        {
            SalesRequestId = id,
            AuthorUserId = userId.Value,
            Note = note,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        InvalidateMetricsCache();
        return NoContent();
    }

    [HttpPost("{id:int}/attachments")]
    [Authorize(Roles = "Sales,Master")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<ActionResult<object>> UploadAttachment(int id, IFormFile file)
    {
        // Validate request exists
        var r = await _context.SalesRequests.FindAsync(id);
        if (r == null) return NotFound(new { message = "Sales request not found" });

        // Validate file using security service
        var (isValid, errorMessage) = await _fileValidationService.ValidateUploadAsync(file);
        if (!isValid)
        {
            _logger.LogWarning("File upload validation failed for request {RequestId}: {Error}", id, errorMessage);
            return BadRequest(new { message = errorMessage });
        }

        // Sanitize filename
        var originalFileName = Path.GetFileName(file.FileName);
        var sanitizedFileName = SanitizeFileName(originalFileName);
        
        // Generate unique filename
        var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        // Create upload directory
        var uploadDir = Path.Combine("wwwroot", "uploads", "sales-requests", id.ToString());
        Directory.CreateDirectory(uploadDir);

        // Full path
        var filePath = Path.Combine(uploadDir, uniqueFileName);

        // Calculate SHA256 hash while saving
        string sha256Hash;
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            sha256Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // Check for duplicate files (same hash)
        var existingFile = await _context.SalesRequestAttachments
            .FirstOrDefaultAsync(a => a.SalesRequestId == id && a.Sha256 == sha256Hash);
        
        if (existingFile != null)
        {
            // Delete the newly uploaded duplicate
            System.IO.File.Delete(filePath);
            _logger.LogInformation("Duplicate file detected for request {RequestId}, returning existing attachment", id);
            return Ok(new
            {
                id = existingFile.Id,
                fileName = existingFile.FileName,
                mimeType = existingFile.MimeType,
                sizeBytes = existingFile.SizeBytes,
                uploadedAt = existingFile.UploadedAt,
                isDuplicate = true
            });
        }

        // Create database record
        var attachment = new SalesRequestAttachment
        {
            SalesRequestId = id,
            FileName = sanitizedFileName,
            StoragePath = filePath,
            MimeType = file.ContentType,
            SizeBytes = file.Length,
            Sha256 = sha256Hash,
            UploadedAt = DateTime.UtcNow
        };

        _context.SalesRequestAttachments.Add(attachment);
        
        // Add timeline event
        _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
        {
            SalesRequestId = id,
            Type = "attachment",
            Description = $"Attachment uploaded: {sanitizedFileName}",
            HappenedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Attachment uploaded for sales request {RequestId}: {FileName}", id, sanitizedFileName);

        return Created($"/api/sales/requests/{id}/attachments/{attachment.Id}", new
        {
            id = attachment.Id,
            fileName = attachment.FileName,
            mimeType = attachment.MimeType,
            sizeBytes = attachment.SizeBytes,
            uploadedAt = attachment.UploadedAt
        });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string SanitizeInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove potentially dangerous characters
        return input.Trim()
            .Replace("<", "")
            .Replace(">", "")
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove path traversal attempts and invalid characters
        var sanitized = Path.GetFileName(fileName);
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", sanitized.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void InvalidateMetricsCache()
    {
        _cache.Remove("sales_kpis");
        // Remove all trend and revenue cache entries
        var keys = new[] { "month", "week", "year" };
        var months = new[] { 3, 6, 12 };
        foreach (var key in keys)
        {
            foreach (var m in months)
            {
                _cache.Remove($"sales_trends_{key}_{m}");
                _cache.Remove($"sales_revenue_{key}_{m}");
            }
        }
        _logger.LogDebug("Metrics cache invalidated");
    }
}
