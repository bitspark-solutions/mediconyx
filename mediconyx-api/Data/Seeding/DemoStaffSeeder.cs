using Microsoft.EntityFrameworkCore;
using mediconyx_api.Models;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Data.Seeding;

/// <summary>
/// Seeds the demo hospital Nurse + Accountant. Idempotent — runs even if the
/// tenant already exists, because these staff were added after the initial
/// hospital seed.
/// </summary>
public static class DemoStaffSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, IPasswordService passwordService)
    {
        var staffTenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Subdomain == "city-general");
        if (staffTenant != null)
        {
            var nurseEmail = "nurse@citygeneral.local";
            if (await dbContext.Users.FirstOrDefaultAsync(u => u.Email == nurseEmail) == null)
            {
                dbContext.Users.Add(new User
                {
                    Email = nurseEmail,
                    PasswordHash = passwordService.HashPassword("Nurse@123"),
                    FirstName = "Ayesha",
                    LastName = "Siddiqua",
                    PhoneNumber = "+880-1700-000020",
                    Role = UserRole.Nurse,
                    TenantId = staffTenant.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }

            var accountantEmail = "accountant@citygeneral.local";
            if (await dbContext.Users.FirstOrDefaultAsync(u => u.Email == accountantEmail) == null)
            {
                dbContext.Users.Add(new User
                {
                    Email = accountantEmail,
                    PasswordHash = passwordService.HashPassword("Accountant@123"),
                    FirstName = "Mahmud",
                    LastName = "Karim",
                    PhoneNumber = "+880-1700-000021",
                    Role = UserRole.Accountant,
                    TenantId = staffTenant.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
