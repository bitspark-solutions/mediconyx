using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// Join table — which permissions does each role have by default.
/// Seeded once on first migration. Per-tenant overrides come later if needed.
/// </summary>
public class RolePermission
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
