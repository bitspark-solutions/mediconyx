# 2026-07-21 — Program.cs & ApplicationDbContext slimming executed

## What was done

Executed `.plan/2026-07-21_program_and_dbcontext_slimming.md` (pure refactor, zero behavior change).

### A. ApplicationDbContext split
- Created `mediconyx-api/Data/Configurations/` with 33 files, one `IEntityTypeConfiguration<T>` per entity.
- Merged the two `Entity<User>(...)` blocks (old lines 70 & 449) into `UserConfiguration.cs` and the two `Entity<Department>(...)` blocks (old lines 265 & 459) into `DepartmentConfiguration.cs`.
- `Data/ApplicationDbContext.cs` reduced to DbSets + ctor + `OnModelCreating` calling `modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly)` (~90 lines).

### B. Program.cs slimmed (622 → 14 lines)
- `Data/Seeding/PlatformSeeder.cs` — Platform tenant, Master user (MasterUser:* config), sales@/admin@mediconyx.local (Dev/Test gate preserved inside).
- `Data/Seeding/DemoHospitalSeeder.cs` — City General Hospital tenant, admin/receptionist, 3 departments, 3 doctors + profiles, 4 patients (+users). Exact order and per-entity SaveChangesAsync preserved; credentials/PatientNumbers untouched.
- `Data/Seeding/DemoStaffSeeder.cs` — Nurse + Accountant idempotent block.
- `Data/Seeding/DatabaseInitializer.cs` — `InitializeDatabaseAsync(this WebApplication)`: Dev/Test gate → MigrateAsync → PlatformSeeder → (Dev/Test) DemoHospitalSeeder → DemoStaffSeeder → PermissionSeeder.SeedAsync, try/catch-log preserved. Logger via `ILoggerFactory.CreateLogger(typeof(DatabaseInitializer).FullName!)` because static classes cannot be used as `ILogger<T>` type arguments (CS0718); category name identical to `ILogger<DatabaseInitializer>`.
- `Extensions/ServiceCollectionExtensions.cs` — `AddMediconyxServices(config, env)`: all DI verbatim, including the 🧠/📊 Console.WriteLine lines and both AddMemoryCache() calls.
- `Extensions/WebApplicationExtensions.cs` — `UseMediconyxPipeline()`: dev Swagger, container HTTPS-redirect skip, CorrelationId, GlobalExceptionHandler, CORS, Test-env rate-limit skip, auth, MapControllers.
- `Program.cs` is now the 6-statement composition root from the plan.

## Verification

- `dotnet build mediconyx-api/mediconyx-api.csproj` — 0 warnings, 0 errors.
- `dotnet ef migrations has-pending-model-changes` (run from mediconyx-api) — "No changes have been made to the model since the last migration." EF model identical; ModelSnapshot/migrations untouched.
- `dotnet test mediconyx-api/Tests/mediconyx-api.Tests.csproj` — **Passed: 170, Skipped: 1, Failed: 0**. Note: task brief cited a 158+1 baseline; the extra 12 passing tests presumably come from the agent working in parallel on the Tests project (which this change did not touch).

## Deviations from plan

- `ILogger<DatabaseInitializer>` could not be used literally (static class → CS0718); replaced with `ILoggerFactory.CreateLogger(typeof(DatabaseInitializer).FullName!)` — same category, same behavior.
- Test count differs from brief baseline (170 vs 158 passing) — external cause, all green.

## Remaining / next steps

- Plan's verification step 3 (docker `down -v && up`, login smoke, E2E suite) not run — docker was out of scope for this task.
