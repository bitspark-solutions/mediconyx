using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using System.Security.Cryptography;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/sales/requests")]
public class SalesRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SalesRequestsController> _logger;

    public SalesRequestsController(ApplicationDbContext context, ILogger<SalesRequestsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("public")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CreatePublic(PublicSalesRequestCreate request)
    {
        if (string.IsNullOrWhiteSpace(request.CaptchaToken))
        {
            return BadRequest(new { message = "Captcha is required." });
        }

        var sr = new SalesRequest
        {
            HospitalName = request.HospitalName,
            ContactPerson = request.ContactPerson,
            Email = request.Email.ToLowerInvariant(),
            Phone = request.Phone,
            Region = request.Region,
            Notes = request.Notes,
            Status = SalesRequestStatus.Pending,
            SourceIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            SubscriptionPlan = request.SubscriptionPlan,
            BillingCycle = request.BillingCycle,
            Currency = request.Currency,
            QuotedPrice = request.QuotedPrice,
            Seats = request.Seats,
            CreatedAt = DateTime.UtcNow
        };

        _context.SalesRequests.Add(sr);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Public sales request created {Id}", sr.Id);
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
        return NoContent();
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

        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        // Validate file size (10MB)
        const long maxSize = 10 * 1024 * 1024;
        if (file.Length > maxSize)
            return BadRequest(new { message = $"File size exceeds {maxSize / 1024 / 1024}MB limit" });

        // Validate file type
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
        var allowedMimeTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/png",
            "image/jpeg"
        };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });

        if (!allowedMimeTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Invalid file content type" });

        // Sanitize filename
        var originalFileName = Path.GetFileName(file.FileName);
        var sanitizedFileName = string.Join("_", originalFileName.Split(Path.GetInvalidFileNameChars()));
        
        // Generate unique filename
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
}
