using Microsoft.EntityFrameworkCore;
using mediconyx_api.Services.Auth;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Data.Seeding;

/// <summary>
/// Runs EF migrations and all startup seeders. Development/Test only.
/// Failures are logged, not thrown, so the app still starts.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        // Run migrations on startup (Development only)
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Test"))
        {
            return;
        }

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                await dbContext.Database.MigrateAsync();
                var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
                var platformTenant = await PlatformSeeder.SeedAsync(
                    dbContext, passwordService, app.Configuration, app.Environment);
                if (platformTenant != null)
                {
                    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
                    {
                        await DemoHospitalSeeder.SeedAsync(dbContext, passwordService);
                        await DemoStaffSeeder.SeedAsync(dbContext, passwordService);
                    }

                    // ── RBAC SEED (Tier 1.1) ────────────────────────────────
                    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(typeof(DatabaseInitializer).FullName!);
                    await PermissionSeeder.SeedAsync(dbContext, seedLogger);
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(DatabaseInitializer).FullName!);
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }
    }
}
