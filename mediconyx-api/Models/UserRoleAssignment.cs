using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

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
