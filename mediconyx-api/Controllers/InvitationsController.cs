using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Models;
using mediconyx_api.Services.Invitations;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationService;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(IInvitationService invitationService, ILogger<InvitationsController> logger)
    {
        _invitationService = invitationService;
        _logger = logger;
    }

    public class ValidateInvitationResponse
    {
        public bool Valid { get; set; }
        public string? Email { get; set; }
        public string? HospitalName { get; set; }
        public string? IntendedRole { get; set; }
        /// <summary>True when accepting immediately activates the account (tenant invite); false for pre-tenant pipeline invites.</summary>
        public bool AccountActivatedOnAccept { get; set; }
    }

    public class AcceptInvitationRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class InvitationDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string IntendedRole { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime TokenExpiry { get; set; }
    }

    // GET /api/invitations/validate?token=...
    [HttpGet("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ValidateInvitationResponse>> Validate([FromQuery] string token, CancellationToken ct)
    {
        var invitation = await _invitationService.ValidateTokenAsync(token, ct);
        if (invitation == null)
            return Ok(new ValidateInvitationResponse { Valid = false });

        return Ok(new ValidateInvitationResponse
        {
            Valid = true,
            Email = invitation.Email,
            HospitalName = invitation.Tenant?.Name ?? invitation.SalesRequest?.HospitalName,
            IntendedRole = invitation.IntendedRole.ToString(),
            AccountActivatedOnAccept = invitation.TenantId.HasValue
        });
    }

    // POST /api/invitations/accept
    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(AcceptInvitationRequest request, CancellationToken ct)
    {
        var result = await _invitationService.AcceptInvitationAsync(request.Token, request.Password, request.ConfirmPassword, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            message = result.Message,
            email = result.Email,
            hospitalName = result.HospitalName,
            accountActivated = result.AccountActivated
        });
    }

    // GET /api/invitations/by-request/{salesRequestId}  (Sales) — invite status for a pipeline request
    [HttpGet("by-request/{salesRequestId:int}")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<ActionResult<List<InvitationDto>>> GetByRequest(int salesRequestId, CancellationToken ct)
    {
        var invitations = await _invitationService.GetBySalesRequestAsync(salesRequestId, ct);
        return Ok(invitations.Select(ToDto).ToList());
    }

    // POST /api/invitations/{id}/resend  (Sales)
    [HttpPost("{id:int}/resend")]
    [Authorize(Roles = "Sales,Master")]
    public async Task<IActionResult> Resend(int id, CancellationToken ct)
    {
        var sent = await _invitationService.ResendAsync(id, ct);
        if (!sent)
            return BadRequest(new { message = "Could not resend this invitation (it may already be accepted, or the email failed to send)." });
        return Ok(new { message = "Invitation resent." });
    }

    private static InvitationDto ToDto(Invitation i) => new()
    {
        Id = i.Id,
        Email = i.Email,
        IntendedRole = i.IntendedRole.ToString(),
        Status = i.Status.ToString(),
        CreatedAt = i.CreatedAt,
        AcceptedAt = i.AcceptedAt,
        TokenExpiry = i.TokenExpiry
    };
}
