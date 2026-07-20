using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Auth;

/// <summary>
/// Idempotent seeder for the global Permission catalogue and the per-role RolePermission matrix.
/// Called from Program.cs after MigrateAsync() and before app.Run() — safe to re-run.
/// </summary>
public static class PermissionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        var existingPerms = await context.Permissions.Select(p => p.Name).ToHashSetAsync();
        var toAdd = PermissionCatalog.All
            .Where(p => !existingPerms.Contains(p.Name))
            .Select(p => new Permission
            {
                Name = p.Name,
                Category = p.Category,
                Description = p.Description,
                CreatedAt = DateTime.UtcNow,
            })
            .ToList();

        if (toAdd.Any())
        {
            context.Permissions.AddRange(toAdd);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} new permissions", toAdd.Count);
        }

        // Reload — we need IDs for the join rows
        var permsByName = await context.Permissions.ToDictionaryAsync(p => p.Name, p => p.Id);

        var existingRolePerms = await context.RolePermissions
            .Select(rp => new { rp.Role, rp.PermissionId })
            .ToListAsync();
        var existingSet = existingRolePerms.Select(x => (x.Role, x.PermissionId)).ToHashSet();

        var toAddRolePerms = new List<RolePermission>();
        foreach (var (role, permNames) in PermissionCatalog.DefaultRolePermissions)
        {
            foreach (var name in permNames)
            {
                if (!permsByName.TryGetValue(name, out var permId)) continue;
                if (existingSet.Contains((role, permId))) continue;
                toAddRolePerms.Add(new RolePermission { Role = role, PermissionId = permId });
            }
        }

        if (toAddRolePerms.Any())
        {
            context.RolePermissions.AddRange(toAddRolePerms);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} new role-permission rows", toAddRolePerms.Count);
        }
    }
}
