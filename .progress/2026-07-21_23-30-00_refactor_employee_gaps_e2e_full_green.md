# 2026-07-21 — Parallel Refactor + Employee Gaps + E2E Full Green (112/112)

Big session. Three workstreams landed and the whole E2E suite is green for the first time on a fresh-volume stack.

## 1. Program.cs / ApplicationDbContext.cs slimming (subagent, reviewed)
- `Program.cs` 622 → **14 lines** (composition root). New: `Extensions/ServiceCollectionExtensions.cs` (all DI), `Extensions/WebApplicationExtensions.cs` (pipeline), `Data/Seeding/` — `PlatformSeeder`, `DemoHospitalSeeder`, `DemoStaffSeeder`, `DatabaseInitializer` (MigrateAsync + seeders + PermissionSeeder, same Dev/Test gating and try/catch behavior).
- `ApplicationDbContext.cs` 604 → **70 lines**. 33 `IEntityTypeConfiguration<T>` classes in `Data/Configurations/` (one per file); duplicate `Entity<User>` and `Entity<Department>` blocks merged. `OnModelCreating` is now just `ApplyConfigurationsFromAssembly`.
- Proven identical EF model: `dotnet ef migrations has-pending-model-changes` → none. Snapshot untouched.

## 2. Employee onboarding gaps closed (subagent, reviewed)
- DTOs expose `Designation`, `PrimaryDepartmentId`, `AdditionalRoles`; `UserService` persists them + syncs `UserRoleAssignment` rows (admin-only, primary role excluded).
- New `POST /api/doctor/attach-profile` — gives an existing staff user a DoctorProfile + Doctor role (director-who-practices). Tenant/specialization/department/duplicate guards.
- New UI `app/portal/admin/employees/page.tsx` (list/create/edit/deactivate, designation, departments, multi-role chips, "Make practicing doctor"), linked from admin tab bar. Re-login hint for JWT-baked permissions.
- +12 unit tests → **170 passed / 1 skipped**.

## 3. E2E repairs (this session, 12 failures → 0)
- **Env-var split bug (real bug)**: `register`, `verify-email`, `doctors`, `doctors/[id]` read `NEXT_PUBLIC_API_URL`; everything else uses `NEXT_PUBLIC_API_BASE_URL`. Standardized all four to the BASE var — was silently breaking those pages on any non-default deployment (incl. the test stack).
- **Register form a11y**: labels now associated via `htmlFor`/`id`; required asterisk moved to a CSS `::after` pseudo-element. (Playwright `getByLabel` matches DOM text and ignores `aria-hidden` — a span asterisk breaks anchored patterns like `/^email$/i`.)
- **tier2-ipd tests made state-independent**: new `ensureWard()` helper (unique code suffix + re-list fallback) — tests previously assumed leftover wards in the test DB and broke on any fresh volume.
- contact-sales `beforeEach` timeouts were test-ui warmup; no code change needed.

## Verification
- `dotnet build` clean; `tsc --noEmit` clean; **170/171 API tests** pass.
- Fresh-volume test stack: migrations + seeding from empty DB verified (login, all seeded users).
- Live smoke: employee create with designation+roles ✅, attach-profile ✅ (+duplicate rejection), cabins unaffected ✅, `/portal/admin/employees` 200 ✅.
- **Full Playwright suite: 112 passed, 0 failed, 3 skipped** (conditional skips).

## Notes / follow-ups
- Dev stack (`docker-compose.yml`) still runs pre-session images; rebuild when convenient.
- Test stack is up with all changes (API 9766, UI 9674).
- Suite is now robust to `down -v` — keep it that way: new tests must self-provision data.
- Remaining story items: P2 multi-location (needs decision), P3 tenant-custom roles (recommended against), cabin E2E coverage (suite has none yet).
