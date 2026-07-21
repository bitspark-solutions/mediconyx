# 2026-07-21 — Multi-Branch Support: Stage 1 (Foundation) Complete

User confirmed the Max Healthcare model as the target: one brand, many branches, shared patient records, per-branch operations. Full design in `.plan/2026-07-21_multibranch_support.md` (shared vs branch-scoped entities, 4 stages, risks).

## Stage 1 delivered (backend foundation)

- `Models/Branch.cs` — Name, Code (unique per tenant, uppercased), Address/City/Phone/Email, `IsMain`, soft-delete via `IsActive`.
- `Data/Configurations/BranchConfiguration.cs` — indexes `(TenantId, Code)` unique, `(TenantId, IsActive)`; cascade from Tenant.
- `DbSet<Branch>` on the (now slim) ApplicationDbContext.
- DTOs: `BranchDto`, `CreateBranchRequest`, `UpdateBranchRequest` (one per file).
- `Controllers/BranchController.cs` (`/api/branch`) — GET list (main-first, `includeInactive` opt), GET by id, POST, PUT, DELETE (soft). Write ops guarded by `TENANT_SETTINGS_EDIT`, reads by `TENANT_SETTINGS_VIEW`. Single-main invariant enforced: first branch auto-main, promoting demotes previous main, main can't be deleted/deactivated.
- Migration `20260721000200_AddBranches` — creates table + **backfills "Main Branch" (code MAIN) for every existing tenant**, copying address/phone/email from the tenant row. Attribute-tagged (`[DbContext]`+`[Migration]`) per the lesson from the cabin migration.
- ModelSnapshot re-synced (scaffold-and-delete trick); `has-pending-model-changes` clean.

## Verification (test stack 9766, fresh images)
- Migration applied; both tenants (Platform + City General) have Main Branch in DB ✅
- Create "City General Uttara" ✅, duplicate code 400 ✅, list main-first ✅, promote Uttara → main (old demoted) ✅, delete main blocked ✅, cross-tenant read → 404 ✅
- `dotnet build` clean; **170/171 API tests pass** (1 pre-existing skip).

## Next stages (not started)
- **Stage 2 — IPD**: `BranchId` on Ward/Bed (+DTOs, bed-board branch filter; relax ward Code uniqueness to per-branch).
- **Stage 3 — Flows**: `BranchId` on Appointment/Admission/Invoice; `PrimaryBranchId` on User + JWT `branch_id` claim; employee UI branch field.
- **Stage 4 — Polish**: branch switcher in header, role-aware default scoping, cross-branch isolation E2E, seed demo 2nd branch.
