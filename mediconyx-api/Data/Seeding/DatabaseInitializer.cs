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
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(DatabaseInitializer).FullName!);
            try
            {
                // Postgres may still be initializing when the API starts (e.g. after a
                // Docker daemon restart, where depends_on healthchecks don't apply).
                // Retry instead of booting against a broken/empty schema.
                await MigrateWithRetryAsync(dbContext, logger);

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

                    // Every tenant must have a main branch (migration backfill only
                    // covers tenants that existed at migrate time).
                    await BranchSeeder.EnsureMainBranchesAsync(dbContext);

                    // ── RBAC SEED (Tier 1.1) ────────────────────────────────
                    await PermissionSeeder.SeedAsync(dbContext, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }
    }

    private static async Task MigrateWithRetryAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning("Database migration attempt {Attempt}/{Max} failed ({Message}); retrying in 3s…",
                    attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}
