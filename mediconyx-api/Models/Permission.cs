using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A single permission flag. Flat list — checked via [HasPermission("admission.create")] on controllers.
/// Permissions are GLOBAL (not tenant-scoped) so the seed matrix applies to all tenants.
/// </summary>
public class Permission
{
    public int Id { get; set; }

    /// <summary>
    /// Stable dotted name used in code & [HasPermission] attributes.
    /// Examples: "admission.create", "admission.discharge", "bed.assign",
    ///           "user.manage", "doctor.profile.edit", "telemedicine.start".
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Coarse category for UI grouping. E.g. "admissions", "billing", "users".
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
