using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/sales/requests/{requestId:int}")]
[Authorize(Roles = "Sales,Admin,Master")]
public class ApprovalController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApprovalController> _logger;
    private readonly IEmailService _emailService;

    public ApprovalController(
        ApplicationDbContext context,
        ILogger<ApprovalController> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    [HttpPost("approve-profile")]
    public async Task<ActionResult<SalesRequest>> ApproveProfile(int requestId)
    {
        var salesRequest = await _context.SalesRequests
            .Include(r => r.HospitalProfile)
            .Include(r => r.TechnicalRequirements)
            .Include(r => r.ModuleSelection)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (salesRequest == null)
        {
            return NotFound(new { message = "Sales request not found." });
        }

        // Validate preconditions
        var validationErrors = new List<string>();

        if (salesRequest.Status != SalesRequestStatus.InProgress)
        {
            validationErrors.Add("Only requests in 'InProgress' status can be approved.");
        }

        if (salesRequest.HospitalProfile == null)
        {
            validationErrors.Add("Hospital profile is required.");
        }

        if (salesRequest.TechnicalRequirements == null)
        {
            validationErrors.Add("Technical requirements are required.");
        }

        if (salesRequest.ModuleSelection == null)
        {
            validationErrors.Add("Module selection is required.");
        }

        // Check mandatory documents (License, TaxCert, BusinessReg)
        // Note: This assumes DocumentType enum exists and is being used
        // For now, we'll check if at least 3 documents are uploaded
        var attachmentCount = salesRequest.Attachments.Count;
        if (attachmentCount < 3)
        {
            validationErrors.Add($"At least 3 mandatory documents are required. Currently {attachmentCount} uploaded.");
        }

        if (validationErrors.Any())
        {
            return BadRequest(new 
            { 
                message = "Profile approval preconditions not met.",
                errors = validationErrors
            });
        }

        // Update status to AwaitingPayment
        salesRequest.Status = SalesRequestStatus.AwaitingPayment;
        salesRequest.UpdatedAt = DateTime.UtcNow;

        var userName = User.Identity?.Name ?? "Admin";

        // Create timeline event
        _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
        {
            SalesRequestId = requestId,
            Type = "Approved",
            Description = $"Profile approved by {userName}",
            HappenedAt = DateTime.UtcNow
        });

        // Create audit log
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(SalesRequest),
            EntityId = requestId.ToString(),
            Action = "approve_profile",
            ActorUserId = GetCurrentUserId(),
            OldValues = SalesRequestStatus.InProgress.ToString(),
            NewValues = SalesRequestStatus.AwaitingPayment.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Send approval email (fire and forget)
        _ = SendProfileApprovalEmail(salesRequest);

        _logger.LogInformation(
            "Hospital profile approved for SalesRequest {RequestId} by {UserName}",
            requestId,
            userName);

        return Ok(salesRequest);
    }

    [HttpPost("reject-profile")]
    public async Task<ActionResult<SalesRequest>> RejectProfile(
        int requestId,
        [FromBody] RejectProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Rejection reason is required." });
        }

        var salesRequest = await _context.SalesRequests
            .Include(r => r.HospitalProfile)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (salesRequest == null)
        {
            return NotFound(new { message = "Sales request not found." });
        }

        if (salesRequest.Status != SalesRequestStatus.InProgress)
        {
            return BadRequest(new 
            { 
                message = "Only requests in 'InProgress' status can have profile rejected." 
            });
        }

        // Keep status as InProgress (they need to fix and resubmit)
        salesRequest.UpdatedAt = DateTime.UtcNow;

        var userName = User.Identity?.Name ?? "Admin";

        // Create timeline event with rejection details
        var changesDescription = request.RequiredChanges.Any() 
            ? $" Required changes: {string.Join(", ", request.RequiredChanges)}"
            : "";

        _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
        {
            SalesRequestId = requestId,
            Type = "Rejected",
            Description = $"Profile rejected by {userName}: {request.Reason}{changesDescription}",
            HappenedAt = DateTime.UtcNow
        });

        // Create audit log
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(SalesRequest),
            EntityId = requestId.ToString(),
            Action = "reject_profile",
            ActorUserId = GetCurrentUserId(),
            Reason = request.Reason,
            NewValues = string.Join("; ", request.RequiredChanges),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Send rejection email (fire and forget)
        _ = SendProfileRejectionEmail(salesRequest, request.Reason, request.RequiredChanges);

        _logger.LogInformation(
            "Hospital profile rejected for SalesRequest {RequestId} by {UserName}: {Reason}",
            requestId,
            userName,
            request.Reason);

        return Ok(salesRequest);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task SendProfileApprovalEmail(SalesRequest salesRequest)
    {
        try
        {
            var email = salesRequest.HospitalProfile?.PrimaryContactEmail ?? salesRequest.Email;
            var subject = "Hospital Profile Approved - Next Steps";
            var body = $@"
                <h2>Profile Approved</h2>
                <p>Dear {salesRequest.ContactPerson},</p>
                <p>Great news! Your hospital profile for <strong>{salesRequest.HospitalName}</strong> has been approved.</p>
                <h3>Next Steps:</h3>
                <ol>
                    <li>Review the pricing details sent by your sales representative</li>
                    <li>Complete the payment process</li>
                    <li>Upon payment confirmation, we will provision your tenant and send login credentials</li>
                </ol>
                <p>If you have any questions, please contact your sales representative.</p>
                <p>Best regards,<br/>Mediconyx Team</p>
            ";

            await _emailService.SendEmailAsync(email, subject, body);

            // Log email sent in timeline
            _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
            {
                SalesRequestId = salesRequest.Id,
                Type = "EmailSent",
                Description = $"Approval email sent to {email}",
                HappenedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send profile approval email for request {RequestId}", salesRequest.Id);
        }
    }

    private async Task SendProfileRejectionEmail(
        SalesRequest salesRequest, 
        string reason, 
        List<string> requiredChanges)
    {
        try
        {
            var email = salesRequest.HospitalProfile?.PrimaryContactEmail ?? salesRequest.Email;
            var subject = "Hospital Profile Requires Updates";
            
            var changesHtml = requiredChanges.Any()
                ? $"<h3>Required Changes:</h3><ul>{string.Join("", requiredChanges.Select(c => $"<li>{c}</li>"))}</ul>"
                : "";

            var body = $@"
                <h2>Profile Update Required</h2>
                <p>Dear {salesRequest.ContactPerson},</p>
                <p>We have reviewed your hospital profile for <strong>{salesRequest.HospitalName}</strong>.</p>
                <p>Unfortunately, we need you to make some updates before we can proceed.</p>
                <h3>Reason:</h3>
                <p>{reason}</p>
                {changesHtml}
                <p>Please log in to your account and update the required information. Once updated, we will review it again promptly.</p>
                <p>If you have any questions, please contact your sales representative.</p>
                <p>Best regards,<br/>Mediconyx Team</p>
            ";

            await _emailService.SendEmailAsync(email, subject, body);

            // Log email sent in timeline
            _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
            {
                SalesRequestId = salesRequest.Id,
                Type = "EmailSent",
                Description = $"Rejection email sent to {email}",
                HappenedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send profile rejection email for request {RequestId}", salesRequest.Id);
        }
    }
}
