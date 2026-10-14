# P0 Build Fix + README — mediconyx — Progress Report (2026-10-15)

## Before
- `master` did not compile (2 errors from accounting commit `83c5cf5`) → the 12-file xUnit suite ran ZERO tests.
- `readme.md` was literally a table of 13 seeded dev passwords.
- `appsettings.json` + `appsettings.Development.json` (dev secrets) were force-tracked via gitignore negations; `Tests/bin-clean/` artifacts committed.

## Done (commit `c4a749c`, pushed to bitspark-solutions/mediconyx master)
1. Fix: `AdminKpiService.cs:189` `WardType` → `WardTypes.Maternity` (string const class).
2. Fix: `LedgerService.cs:640` `User.FullName` → `FirstName + " " + LastName`.
3. **Verified: `dotnet build` 0 errors; `dotnet test` → 176 passed / 0 failed / 1 skipped.**
4. New `README.md`: hero, honest what's-real vs roadmap matrix (Pharmacy/Lab/Radiology/Emergency/Inventory/HR marked 🚧 until Phase 2 builds them — D3), quick start with real ports (UI 9673, API 9765, storage 8890, db 9543), demo-accounts section reframed as local-dev seeds, architecture, security notes.
5. Untracked `appsettings.json`/`appsettings.Development.json` (local copies kept), fixed gitignore so only `.example`/`Test.json` are tracked; `Tests/bin-clean/` untracked + ignored.
6. Remaining known drift for Phase 1: `PROJECT_OVERVIEW.md` still says ASP.NET 8 / Next 14 / MinIO (actual .NET 10 / Next 16 / custom storage-service); NU1902 warnings (System.Security.Cryptography.Pkcs 9.0.0) — package bump when Phase 2 touches csproj; JWT/DB dev placeholders remain in old history (dev-only creds, no rewrite needed).
