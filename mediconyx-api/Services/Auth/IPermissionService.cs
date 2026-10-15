using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Middleware;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Auth;

/// <summary>
/// Centralized permission check. Reads role from JWT claim "role", then loads
/// the RolePermission set from the DB. We use a per-request cache so a single
/// controller with multiple [HasPermission] attributes doesn't re-query.
/// </summary>
public interface IPermissionService
{
    /// <summary>True if the user (identified by role claim) has the named permission.</summary>
    bool HasPermission(ClaimsPrincipal user, string permission);

    /// <summary>All permission names for the user's role (used when building JWT claims at login).</summary>
    Task<IReadOnlyList<string>> GetPermissionsForRoleAsync(UserRole role);

    /// <summary>Union of permissions across the user's primary role + all additional roles.</summary>
    Task<IReadOnlyList<string>> GetPermissionsForUserAsync(User user);
}
