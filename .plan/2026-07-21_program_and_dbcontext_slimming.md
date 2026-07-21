# 2026-07-21 — Slimming Program.cs & ApplicationDbContext.cs

## Problem

- `Program.cs` — 622 lines. ~430 of them (69%) are **seed data** (Platform tenant, master/sales/admin users, City General Hospital demo: staff, departments, 3 doctors, 4 patients, nurse/accountant), plus DI registration, JWT/CORS/RBAC/rate-limit setup, pipeline, and migration+seed orchestration — all in one top-level file.
- `Data/ApplicationDbContext.cs` — 604 lines. 35 `DbSet`s + **33 inline Fluent entity configurations**, including two separate `Entity<User>(...)` blocks (lines 70 and 449) that are easy to edit in one place and forget in the other.

Both files violate SRP, make merge conflicts likely, and hide the actually-important startup logic.

## Approach (pure refactor — zero behavior change)

### A. ApplicationDbContext → per-entity configuration classes

EF Core's standard solution, and it aligns with the one-class-per-file rule.

1. Create `Data/Configurations/` with one file per entity, each implementing `IEntityTypeConfiguration<T>`:
   - `UserConfiguration.cs`, `TenantConfiguration.cs`, `SalesRequestConfiguration.cs`, `WardConfiguration.cs`, `BedConfiguration.cs`, `AdmissionConfiguration.cs`, … (33 files)
   - The two `Entity<User>` blocks merge into a single `UserConfiguration`.
2. `OnModelCreating` becomes:
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       base.OnModelCreating(modelBuilder);
       modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
   }
   ```
3. Result: `ApplicationDbContext.cs` ≈ 90 lines (DbSets + ctor + OnModelCreating). Each entity's mapping is discoverable next to its peers, and adding a new entity = adding one file.

**Risk**: low — the generated model must be byte-identical. Verify by regenerating the model snapshot and diffing it against the current one (any difference = a config was mistranscribed).

### B. Program.cs → startup composition root (~40 lines)

Split by responsibility into `Data/Seeding/` and `Extensions/`:

| New file (one class each) | Contents |
|---|---|
| `Data/Seeding/PlatformSeeder.cs` | Platform tenant, Master user (from `MasterUser:*` config), sales@/admin@mediconyx.local |
| `Data/Seeding/DemoHospitalSeeder.cs` | City General Hospital tenant, admin/receptionist, 3 departments, 3 doctors + profiles, 4 patients (+users) |
| `Data/Seeding/DemoStaffSeeder.cs` | Nurse + Accountant (currently a separate idempotent block) |
| `Data/Seeding/DatabaseInitializer.cs` | Orchestrates: `MigrateAsync()` → PlatformSeeder → DemoHospitalSeeder → DemoStaffSeeder → `PermissionSeeder.SeedAsync`, preserving the current try/catch-and-log behavior and Dev/Test-only gating |
| `Extensions/ServiceCollectionExtensions.cs` | `AddMediconyxServices(config, env)`: controllers, Swagger, DbContext, JWT auth, CORS, all scoped services, HttpClients, LLM/Embedding, RBAC policies loop, rate limiting |
| `Extensions/WebApplicationExtensions.cs` | `UseMediconyxPipeline(env)`: Swagger (dev), HTTPS-redirect skip in container, CorrelationId, GlobalExceptionHandler, CORS, rate-limit skip in Test, auth, MapControllers |

Result: `Program.cs` ≈
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMediconyxServices(builder.Configuration, builder.Environment);
var app = builder.Build();
app.UseMediconyxPipeline();
await app.InitializeDatabaseAsync();   // Dev/Test only, inside initializer
app.Run();
```

**Risks / watch-outs**:
- Seed order matters (tenant → users → departments → doctors → patients); keep exact order and per-entity `SaveChangesAsync` semantics — the seeded IDs/PatientNumbers are depended on by the E2E suite.
- Seeder console output (`🧠 LLM Provider...` lines) must stay to preserve current ops behavior (or deliberately drop — flag in review).
- `ILogger<Program>` used by the seed block → becomes `ILogger<DatabaseInitializer>`.
- The extension methods need `IConfiguration`/`IWebHostEnvironment` params — keep all current defaults identical.

## Verification
1. `dotnet build` + full test suite (158 tests).
2. `dotnet ef migrations has-pending-model-changes` (or snapshot regen diff) — proves the EF model is unchanged after the DbContext split.
3. Rebuild test-api container → fresh DB volume (`down -v && up`) → startup seeds cleanly, login + smoke calls pass, E2E suite green.

## Effort
~40 new files, mostly mechanical cut-paste. Est. one focused session. No schema changes, no API changes.
