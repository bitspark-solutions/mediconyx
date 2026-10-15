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

    /// <summary>
    /// The user's home branch. Null = tenant-wide (e.g. admins who work across branches).
    /// Baked into the JWT as the `branch_id` claim at login (Stage 3 multi-branch).
    /// </summary>
    public int? PrimaryBranchId { get; set; }
    public Branch? PrimaryBranch { get; set; }

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
