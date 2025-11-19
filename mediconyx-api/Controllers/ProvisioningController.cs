using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Services;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/sales/requests/{requestId:int}")]
[Authorize(Roles = "Admin,Master")]
public class ProvisioningController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvisioningService _provisioningService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProvisioningController> _logger;

    public ProvisioningController(
        ApplicationDbContext context,
        ITenantProvisioningService provisioningService,
        IEmailService emailService,
        ILogger<ProvisioningController> logger)
    {
        _context = context;
        _provisioningService = provisioningService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("provision")]
    public async Task<ActionResult<ProvisioningResult>> ProvisionTenant(int requestId)
    {
        // Load sales request
        var salesRequest = await _context.SalesRequests
            .Include(r => r.HospitalProfile)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (salesRequest == null)
        {
            return NotFound(new { message = "Sales request not found." });
        }

        // Validate preconditions
        var validationErrors = new List<string>();

        if (salesRequest.Status != SalesRequestStatus.AwaitingPayment)
        {
            validationErrors.Add($"Request must be in 'AwaitingPayment' status. Current status: {salesRequest.Status}");
        }

        if (salesRequest.PaymentStatus != PaymentStatus.Paid)
        {
            validationErrors.Add($"Payment must be confirmed before provisioning. Current status: {salesRequest.PaymentStatus}");
        }

        if (salesRequest.TenantId.HasValue)
        {
            validationErrors.Add("Tenant already provisioned for this request.");
        }

        if (validationErrors.Any())
        {
            return BadRequest(new 
            { 
                message = "Provisioning preconditions not met.",
                errors = validationErrors
            });
        }

        // Update status to Provisioning (indicates in-progress)
        salesRequest.Status = SalesRequestStatus.Provisioning;
        salesRequest.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Starting tenant provisioning for SalesRequest {RequestId}", requestId);

        // Call provisioning service
        var result = await _provisioningService.ProvisionTenantAsync(requestId);

        if (result.Success)
        {
            _logger.LogInformation(
                "Tenant provisioning successful for SalesRequest {RequestId}. TenantId: {TenantId}, AdminUserId: {AdminUserId}",
                requestId,
                result.TenantId,
                result.AdminUserId);

            // Send welcome email with credentials (fire and forget)
            if (!string.IsNullOrEmpty(result.AdminEmail) && !string.IsNullOrEmpty(result.TemporaryPassword))
            {
                _ = SendWelcomeEmail(
                    salesRequest.HospitalProfile?.HospitalLegalName ?? salesRequest.HospitalName,
                    result.AdminEmail,
                    result.TemporaryPassword,
                    result.Subdomain ?? string.Empty,
                    requestId);
            }

            return Ok(new
            {
                success = result.Success,
                tenantId = result.TenantId,
                subdomain = result.Subdomain,
                adminEmail = result.AdminEmail,
                message = "Tenant provisioned successfully. Welcome email sent with temporary credentials."
            });
        }
        else
        {
            // Provisioning failed - revert status to AwaitingPayment
            salesRequest.Status = SalesRequestStatus.AwaitingPayment;
            salesRequest.UpdatedAt = DateTime.UtcNow;

            _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
            {
                SalesRequestId = requestId,
                Type = "ProvisioningFailed",
                Description = $"Provisioning failed: {result.ErrorMessage}",
                HappenedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            _logger.LogError(
                "Tenant provisioning failed for SalesRequest {RequestId}: {Error}",
                requestId,
                result.ErrorMessage);

            return StatusCode(500, new
            {
                success = false,
                message = "Provisioning failed. Please try again or contact support.",
                error = result.ErrorMessage,
                warnings = result.Warnings
            });
        }
    }

    private async Task SendWelcomeEmail(
        string hospitalName,
        string adminEmail,
        string temporaryPassword,
        string subdomain,
        int requestId)
    {
        try
        {
            var subject = "Welcome to Mediconyx - Your Account is Ready!";
            var loginUrl = $"https://{subdomain}.mediconyx.com/login";
            
            var body = $@"
                <h1>Welcome to Mediconyx!</h1>
                <p>Dear Administrator,</p>
                <p>Congratulations! Your Mediconyx account for <strong>{hospitalName}</strong> has been successfully provisioned and is ready to use.</p>
                
                <h2>Your Login Credentials</h2>
                <ul>
                    <li><strong>Login URL:</strong> <a href=""{loginUrl}"">{loginUrl}</a></li>
                    <li><strong>Email:</strong> {adminEmail}</li>
                    <li><strong>Temporary Password:</strong> <code>{temporaryPassword}</code></li>
                </ul>
                
                <p><strong style=""color: #d9534f;"">Important:</strong> For security reasons, you will be required to change your password upon first login.</p>
                
                <h2>Getting Started</h2>
                <ol>
                    <li>Click the login URL above or copy it to your browser</li>
                    <li>Enter your email and temporary password</li>
                    <li>Follow the prompts to set a new secure password</li>
                    <li>Explore your dashboard and start configuring your hospital management system</li>
                </ol>
                
                <h2>Need Help?</h2>
                <p>Our support team is here to assist you:</p>
                <ul>
                    <li>Email: support@mediconyx.com</li>
                    <li>Documentation: <a href=""https://docs.mediconyx.com"">docs.mediconyx.com</a></li>
                    <li>Training Videos: <a href=""https://training.mediconyx.com"">training.mediconyx.com</a></li>
                </ul>
                
                <p>Thank you for choosing Mediconyx. We're excited to have you on board!</p>
                
                <p>Best regards,<br/>
                The Mediconyx Team</p>
                
                <hr>
                <p style=""font-size: 12px; color: #777;"">
                    This email contains sensitive information. Please do not forward it to anyone. 
                    If you did not request this account, please contact us immediately at security@mediconyx.com.
                </p>
            ";

            await _emailService.SendEmailAsync(adminEmail, subject, body);

            // Log email sent in timeline
            _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
            {
                SalesRequestId = requestId,
                Type = "EmailSent",
                Description = $"Welcome email sent to {adminEmail} with login credentials",
                HappenedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Welcome email sent to {Email} for SalesRequest {RequestId}", adminEmail, requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email} for SalesRequest {RequestId}", adminEmail, requestId);
            // Don't throw - email failure shouldn't break provisioning
        }
    }
}
