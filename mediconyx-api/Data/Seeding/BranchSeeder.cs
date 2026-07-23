using Microsoft.EntityFrameworkCore;
using mediconyx_api.Models;

namespace mediconyx_api.Data.Seeding;

/// <summary>
/// Guarantees every tenant has exactly one main branch. Heals tenants created before
/// branches existed (the AddBranches migration backfill only covers tenants present
/// at migrate time — fresh databases are seeded AFTER migrations, so their tenants
/// would otherwise never get one).
/// </summary>
public static class BranchSeeder
{
    public static async Task EnsureMainBranchesAsync(ApplicationDbContext db)
    {
        var tenants = await db.Tenants.ToListAsync();
        foreach (var tenant in tenants)
        {
            var branches = await db.Branches.Where(b => b.TenantId == tenant.Id).ToListAsync();
            if (branches.Any(b => b.IsMain)) continue;

            if (branches.Count > 0)
            {
                // Branches exist but none is main — promote the first.
                branches[0].IsMain = true;
                branches[0].UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.Branches.Add(new Branch
                {
                    TenantId = tenant.Id,
                    Name = "Main Branch",
                    Code = "MAIN",
                    Address = tenant.Address,
                    Phone = tenant.ContactPhone,
                    Email = tenant.ContactEmail,
                    IsMain = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
