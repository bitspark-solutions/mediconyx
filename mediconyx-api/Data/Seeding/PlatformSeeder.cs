using Microsoft.EntityFrameworkCore;
using mediconyx_api.Models;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Data.Seeding;

/// <summary>
/// Seeds the Platform tenant, the Master user (from MasterUser:* config),
/// and the development sales@/admin@mediconyx.local users.
/// Returns the Platform tenant, or null when Master credentials are not configured.
/// </summary>
public static class PlatformSeeder
{
    public static async Task<Tenant?> SeedAsync(
        ApplicationDbContext dbContext,
        IPasswordService passwordService,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var masterSection = configuration.GetSection("MasterUser");
        var masterEmail = masterSection["Email"];
        var masterPassword = masterSection["Password"];
        var masterFirstName = masterSection["FirstName"] ?? "Master";
        var masterLastName = masterSection["LastName"] ?? "User";
        if (string.IsNullOrWhiteSpace(masterEmail) || string.IsNullOrWhiteSpace(masterPassword))
        {
            return null;
        }

        var platformTenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Name == "Platform");
        if (platformTenant == null)
        {
            platformTenant = new Tenant { Name = "Platform", IsActive = true, CreatedAt = DateTime.UtcNow };
            dbContext.Tenants.Add(platformTenant);
            await dbContext.SaveChangesAsync();
        }
        var existingMaster = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == masterEmail.ToLower());
        if (existingMaster == null)
        {
            var user = new User
            {
                Email = masterEmail.ToLowerInvariant(),
                PasswordHash = passwordService.HashPassword(masterPassword),
                FirstName = masterFirstName,
                LastName = masterLastName,
                Role = UserRole.Master,
                TenantId = platformTenant.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        // Seed a Sales user for development testing
        if (environment.IsDevelopment() || environment.IsEnvironment("Test"))
        {
            var salesEmail = "sales@mediconyx.local";
            var existingSales = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == salesEmail);
            if (existingSales == null)
            {
                var salesUser = new User
                {
                    Email = salesEmail,
                    PasswordHash = passwordService.HashPassword("Sales@123"),
                    FirstName = "Sales",
                    LastName = "User",
                    Role = UserRole.Sales,
                    TenantId = platformTenant.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Users.Add(salesUser);
                await dbContext.SaveChangesAsync();
            }

            // Also seed an Admin user for testing
            var adminEmail = "admin@mediconyx.local";
            var existingAdmin = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new User
                {
                    Email = adminEmail,
                    PasswordHash = passwordService.HashPassword("Admin@123"),
                    FirstName = "Admin",
                    LastName = "User",
                    Role = UserRole.Admin,
                    TenantId = platformTenant.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Users.Add(adminUser);
                await dbContext.SaveChangesAsync();
            }
        }

        return platformTenant;
    }
}
