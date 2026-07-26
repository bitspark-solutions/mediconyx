using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Services.Invitations;

public interface IInvitationService
{
    /// <summary>Create a pre-tenant pipeline invite tied to a SalesRequest (IntendedRole=Admin) and email the link.</summary>
    Task<Invitation> CreatePipelineInvitationAsync(SalesRequest request, int? invitedByUserId, CancellationToken ct = default);

    /// <summary>Create a tenant self-service invite (tenant already exists) and email the link.</summary>
    Task<Invitation> CreateTenantInvitationAsync(int tenantId, string email, UserRole role, int? invitedByUserId, CancellationToken ct = default);

    /// <summary>Validate a token. Returns the invitation if valid (Pending + not expired), else null.</summary>
    Task<Invitation?> ValidateTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Accept an invite: store the chosen password hash and mark Accepted. For tenant invites, also create the user.</summary>
    Task<AcceptInvitationResult> AcceptInvitationAsync(string token, string password, string confirmPassword, CancellationToken ct = default);

    /// <summary>Regenerate the token + resend the invite email.</summary>
    Task<bool> ResendAsync(int invitationId, CancellationToken ct = default);

    /// <summary>All invitations for a given pipeline request (most recent first).</summary>
    Task<List<Invitation>> GetBySalesRequestAsync(int salesRequestId, CancellationToken ct = default);

    /// <summary>Find the latest Accepted (not yet consumed) pipeline invite for an email — used at provisioning.</summary>
    Task<Invitation?> GetAcceptedPipelineInvitationForEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Mark an invitation consumed once its password hash has been used to create a user.</summary>
    Task MarkConsumedAsync(int invitationId, int createdUserId, CancellationToken ct = default);
}

public class AcceptInvitationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? HospitalName { get; set; }
    /// <summary>True when the account is immediately usable (tenant self-service invite); false for pre-tenant pipeline invites.</summary>
    public bool AccountActivated { get; set; }
}

public class InvitationService : IInvitationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<InvitationService> _logger;

    // Invite links expire 72h after issue.
    private static readonly TimeSpan InviteTokenLifetime = TimeSpan.FromHours(72);
    private const string DefaultPublicBaseUrl = "http://localhost:9673";

    public InvitationService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        IEmailService emailService,
        IConfiguration config,
        ILogger<InvitationService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    public async Task<Invitation> CreatePipelineInvitationAsync(SalesRequest request, int? invitedByUserId, CancellationToken ct = default)
    {
        // Only one live (Pending) invite per request — revoke any earlier ones.
        var existing = await _context.Invitations
            .Where(i => i.SalesRequestId == request.Id && i.Status == InvitationStatus.Pending)
            .ToListAsync(ct);
        foreach (var old in existing)
        {
            old.Status = InvitationStatus.Revoked;
            old.UpdatedAt = DateTime.UtcNow;
        }

        var invitation = new Invitation
        {
            SalesRequestId = request.Id,
            TenantId = null,
            Email = request.Email.Trim().ToLowerInvariant(),
            IntendedRole = UserRole.Admin,
            Token = GenerateToken(),
            TokenExpiry = DateTime.UtcNow.Add(InviteTokenLifetime),
            Status = InvitationStatus.Pending,
            InvitedByUserId = invitedByUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        await SendInviteEmailBestEffortAsync(invitation, request.HospitalName, ct);
        _logger.LogInformation("Pipeline invitation {Id} created for request {RequestId} ({Email})", invitation.Id, request.Id, invitation.Email);
        return invitation;
    }

    public async Task<Invitation> CreateTenantInvitationAsync(int tenantId, string email, UserRole role, int? invitedByUserId, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new InvalidOperationException("Tenant not found.");

        var normalized = email.Trim().ToLowerInvariant();

        // Don't invite an email that already has an account.
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == normalized, ct);
        if (emailTaken) throw new InvalidOperationException("A user with this email already exists.");

        // Revoke any earlier pending invite for the same email+tenant.
        var existing = await _context.Invitations
            .Where(i => i.TenantId == tenantId && i.Email == normalized && i.Status == InvitationStatus.Pending)
            .ToListAsync(ct);
        foreach (var old in existing)
        {
            old.Status = InvitationStatus.Revoked;
            old.UpdatedAt = DateTime.UtcNow;
        }

        var invitation = new Invitation
        {
            SalesRequestId = null,
            TenantId = tenantId,
            Email = normalized,
            IntendedRole = role,
            Token = GenerateToken(),
            TokenExpiry = DateTime.UtcNow.Add(InviteTokenLifetime),
            Status = InvitationStatus.Pending,
            InvitedByUserId = invitedByUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        await SendInviteEmailBestEffortAsync(invitation, tenant.Name, ct);
        _logger.LogInformation("Tenant invitation {Id} created for tenant {TenantId} ({Email}, role {Role})", invitation.Id, tenantId, normalized, role);
        return invitation;
    }

    public async Task<Invitation?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var invitation = await _context.Invitations
            .Include(i => i.SalesRequest)
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Token == token, ct);
        if (invitation == null) return null;
        if (invitation.Status != InvitationStatus.Pending) return null;
        if (invitation.TokenExpiry < DateTime.UtcNow)
        {
            // Mark expired lazily.
            invitation.Status = InvitationStatus.Expired;
            invitation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return null;
        }
        return invitation;
    }

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(string token, string password, string confirmPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new AcceptInvitationResult { Success = false, Message = "Missing invitation token." };
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return new AcceptInvitationResult { Success = false, Message = "Password must be at least 8 characters." };
        if (password != confirmPassword)
            return new AcceptInvitationResult { Success = false, Message = "Passwords do not match." };

        var invitation = await ValidateTokenAsync(token, ct);
        if (invitation == null)
            return new AcceptInvitationResult { Success = false, Message = "This invitation link is invalid or has expired." };

        var hospitalName = invitation.Tenant?.Name ?? invitation.SalesRequest?.HospitalName ?? "Mediconyx";
        invitation.PasswordHash = _passwordService.HashPassword(password);
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.UpdatedAt = DateTime.UtcNow;

        // Tenant self-service invite: the tenant already exists, so create the active user now.
        if (invitation.TenantId.HasValue)
        {
            var emailTaken = await _context.Users.AnyAsync(u => u.Email == invitation.Email, ct);
            if (emailTaken)
                return new AcceptInvitationResult { Success = false, Message = "An account with this email already exists." };

            var user = new User
            {
                Email = invitation.Email,
                PasswordHash = invitation.PasswordHash,
                FirstName = invitation.Email.Split('@')[0],
                LastName = string.Empty,
                Role = invitation.IntendedRole,
                TenantId = invitation.TenantId.Value,
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            invitation.CreatedUserId = user.Id;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Tenant invitation {Id} accepted; user {UserId} created in tenant {TenantId}", invitation.Id, user.Id, invitation.TenantId.Value);
            return new AcceptInvitationResult
            {
                Success = true,
                Message = "Your account is ready. You can now log in.",
                Email = invitation.Email,
                HospitalName = hospitalName,
                AccountActivated = true
            };
        }

        // Pre-tenant pipeline invite: just store the password hash; the account is created at provisioning.
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Pipeline invitation {Id} accepted for {Email}; awaiting provisioning", invitation.Id, invitation.Email);
        return new AcceptInvitationResult
        {
            Success = true,
            Message = "Your password has been set. You'll be able to log in once your hospital account is activated.",
            Email = invitation.Email,
            HospitalName = hospitalName,
            AccountActivated = false
        };
    }

    public async Task<bool> ResendAsync(int invitationId, CancellationToken ct = default)
    {
        var invitation = await _context.Invitations
            .Include(i => i.SalesRequest)
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == invitationId, ct);
        if (invitation == null) return false;
        if (invitation.Status == InvitationStatus.Accepted) return false; // already used

        invitation.Token = GenerateToken();
        invitation.TokenExpiry = DateTime.UtcNow.Add(InviteTokenLifetime);
        invitation.Status = InvitationStatus.Pending;
        invitation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        var hospitalName = invitation.Tenant?.Name ?? invitation.SalesRequest?.HospitalName ?? "Mediconyx";
        return await SendInviteEmailBestEffortAsync(invitation, hospitalName, ct);
    }

    public async Task<List<Invitation>> GetBySalesRequestAsync(int salesRequestId, CancellationToken ct = default)
    {
        return await _context.Invitations
            .Where(i => i.SalesRequestId == salesRequestId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Invitation?> GetAcceptedPipelineInvitationForEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _context.Invitations
            .Where(i => i.TenantId == null
                        && i.Email == normalized
                        && i.Status == InvitationStatus.Accepted
                        && i.CreatedUserId == null
                        && i.PasswordHash != null)
            .OrderByDescending(i => i.AcceptedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task MarkConsumedAsync(int invitationId, int createdUserId, CancellationToken ct = default)
    {
        var invitation = await _context.Invitations.FindAsync(new object[] { invitationId }, ct);
        if (invitation == null) return;
        invitation.CreatedUserId = createdUserId;
        invitation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task<bool> SendInviteEmailBestEffortAsync(Invitation invitation, string hospitalName, CancellationToken ct)
    {
        try
        {
            var baseUrl = _config["App:PublicBaseUrl"] ?? DefaultPublicBaseUrl;
            var link = $"{baseUrl}/accept-invite?token={Uri.EscapeDataString(invitation.Token)}";
            var subject = $"You're invited to set up {hospitalName} on Mediconyx";
            var body = $@"
<html><body>
<h2>You've been invited to Mediconyx</h2>
<p>Dear Customer,</p>
<p>You have been invited to set up the administrator account for <strong>{hospitalName}</strong> on Mediconyx.</p>
<p>Click the link below to set your password and activate your account:</p>
<p><a href='{link}'>Set up your account</a></p>
<p>This link expires in 72 hours. If you weren't expecting this invitation, you can safely ignore this email.</p>
<p>Best regards,<br/>Mediconyx Team</p>
</body></html>";
            await _emailService.SendEmailAsync(invitation.Email, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {Email}", invitation.Email);
            return false;
        }
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
