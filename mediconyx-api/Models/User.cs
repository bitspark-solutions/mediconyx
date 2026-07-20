using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Free-form job title / designation. e.g. "Director of Cardiology", "Head Nurse",
    /// "Senior Surgeon", "Reception Supervisor". Independent of Role — a Director
    /// can also be a Doctor, a Nurse can also be a Ward Manager.
    /// </summary>
    [StringLength(150)]
    public string? Designation { get; set; }

    /// <summary>
    /// Optional department this user is primarily assigned to. Useful for heads of dept,
    /// ward managers, etc. Independent of any specific role.
    /// </summary>
    public int? PrimaryDepartmentId { get; set; }
    public Department? PrimaryDepartment { get; set; }

    // Multi-tenant support
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // Refresh token for JWT
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Additional roles held by this user. e.g. Doctor + Admin.</summary>
    public ICollection<UserRoleAssignment> AdditionalRoles { get; set; } = new List<UserRoleAssignment>();

    // ── Email verification (for self-registered patients) ──
    // Staff users are pre-verified (EmailVerified = true on create).
    // Self-registered patients are EmailVerified = false until they click the link.
    public bool EmailVerified { get; set; } = true;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
}

/// <summary>
/// One row per additional role a user holds. e.g. User (primary) = Doctor,
/// UserRoleAssignment (extra) = Admin. The set of (UserRole, UserRoleAssignment.Roles)
/// is the user's full authority surface.
/// </summary>
public class UserRoleAssignment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public UserRole Role { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public int? GrantedByUserId { get; set; }
}


