namespace mediconyx_api.Models;

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}

/// <summary>
/// A secure invitation for a user to join Mediconyx by setting their own password.
/// Two flavours:
///  - Pre-tenant pipeline invite: created when a SalesRequest is Qualified. TenantId is null
///    because the tenant does not exist yet; the invitee sets a password that is later used
///    as the tenant Admin's password at provisioning time.
///  - Tenant self-service invite: created by a tenant Admin to invite a colleague. TenantId is
///    set; accepting the invite immediately creates an active User in that tenant.
/// </summary>
public class Invitation
{
    public int Id { get; set; }

    /// <summary>Pipeline request this invite is tied to (null for tenant self-service invites).</summary>
    public int? SalesRequestId { get; set; }
    public SalesRequest? SalesRequest { get; set; }

    /// <summary>Tenant this invite belongs to (null for pre-tenant pipeline invites).</summary>
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Role the invitee will hold when their account is created.</summary>
    public UserRole IntendedRole { get; set; } = UserRole.Admin;

    /// <summary>URL-safe cryptographic token emailed to the invitee.</summary>
    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpiry { get; set; }

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>Password hash chosen by the invitee when they accept. Null until accepted.</summary>
    public string? PasswordHash { get; set; }
    public DateTime? AcceptedAt { get; set; }

    /// <summary>Set once the invitation has been consumed to create a real user (provisioning or accept).</summary>
    public int? CreatedUserId { get; set; }

    /// <summary>User who triggered the invite (null for automatic pipeline invites).</summary>
    public int? InvitedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
