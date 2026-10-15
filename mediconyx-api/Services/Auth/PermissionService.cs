using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Middleware;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Auth;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (string.IsNullOrEmpty(permission)) return true;
        var role = user.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(role)) return false;
        if (!Enum.TryParse<UserRole>(role, out var roleEnum)) return false;

        // Master is omnipotent by convention.
        if (roleEnum == UserRole.Master) return true;

        // Check the "permissions" claim set (populated at login)
        var permsClaim = user.FindAll("permission").Select(c => c.Value).ToHashSet();
        if (permsClaim.Contains(permission)) return true;

        return false;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsForRoleAsync(UserRole role)
    {
        if (role == UserRole.Master)
        {
            // Master gets every permission
            return PermissionCatalog.All.Select(p => p.Name).ToList();
        }
        return await _context.RolePermissions
            .Where(rp => rp.Role == role)
            .Select(rp => rp.Permission.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Union of permissions from primary role + all additional roles assigned to the user.
    /// Used at login to build the JWT claims. A user with primary role=Doctor and
    /// an extra Admin assignment gets the union of both permission sets.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetPermissionsForUserAsync(User user)
    {
        if (user.Role == UserRole.Master)
            return PermissionCatalog.All.Select(p => p.Name).ToList();

        var roleSet = new HashSet<UserRole> { user.Role };
        if (user.AdditionalRoles != null)
        {
            foreach (var extra in user.AdditionalRoles)
                roleSet.Add(extra.Role);
        }

        return await _context.RolePermissions
            .Where(rp => roleSet.Contains(rp.Role))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
    }
}
