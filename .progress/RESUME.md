# RESUME — Mediconyx Current State

> Stable resume point. **Update this file at the end of every work session.**
> Session history lives in `.progress/<datetime>_<title>.md`; plans in `.plan/`.
> Last updated: 2026-07-21 (late) — multi-branch Stage 1.

## Where we are

Story coverage: master admin ✅, sales→approval→provisioning ✅, hospital profile/logo ✅, employee onboarding + custom designations + multi-role ✅ (NEW), doctor-as-staff duality ✅ (NEW `attach-profile`), IPD wards/beds/admissions/ICU/PICU/daycare ✅, cabins ✅ (NEW), per-bed-day billing ✅.
Big refactor done: `Program.cs` 14 lines, DbContext 70 lines + `Data/Configurations/`, seeders in `Data/Seeding/`.
E2E: **152 passed / 0 failed / 3 skipped** (155 total incl. tier4 branch/cabin/employee specs) — last full green run 2026-07-22.

## Stacks

- **Dev** (docker-compose.yml): UI http://localhost:9673, API http://localhost:9765 (+/swagger), DB 9543, storage 7890, MailHog 8025. Rebuilt with all changes 2026-07-21.
- **Test** (docker-compose.test.yml): UI 9674, API 9766, storage 7891. Fresh-volume, all changes, E2E green.
- Run E2E: `cd ../mediconyx-e2e && npm run test:env`.
- Logins: hospital admin `admin@citygeneral.local`/`Hospital@123`, master `master@mediconyx.local`/`MasterPass123!` (dev) / `Master@123` (test). Full list in `readme.md`.

## Active workstream: multi-branch (Max Healthcare model)

Plan: `.plan/2026-07-21_multibranch_support.md`. Progress logs: `.progress/2026-07-21_23-55-00_multibranch_stage1_foundation.md`.

- ✅ **Stage 1** — Branch entity, `/api/branch` CRUD, migration `20260721000200_AddBranches` with Main-Branch backfill. Verified live.
- ⬜ **Stage 2 — IPD scoping** (RESUME HERE): add `BranchId` to Ward/Bed (+config, migration backfilling tenant's MAIN branch, DTOs, IPDController validation + `?branchId=` filters); relax ward `Code` uniqueness from per-tenant to per-(TenantId,BranchId); bed-board UI branch filter; branch management UI page for admins.
- ⬜ **Stage 3 — Flows**: `BranchId` on Appointment/Admission/Invoice; `PrimaryBranchId` on User + JWT `branch_id` claim; employee UI branch field.
- ⬜ **Stage 4 — Polish**: header branch switcher, role-aware default scoping, cross-branch isolation E2E, seed demo 2nd branch.

## Standing decisions & gotchas

- One class per file (hard rule). No AgentSwarm. Check `usage` before tasks; stop at 95%.
- Hand-written migrations MUST carry `[DbContext(typeof(ApplicationDbContext))]` + `[Migration("id")]` or EF silently ignores them.
- Keep ModelSnapshot in sync: scaffold throwaway migration → delete its files → snapshot stays regenerated. Verify with `dotnet ef migrations has-pending-model-changes`.
- Tenant isolation is manual `.Where(TenantId == ...)`; BranchId filtering must follow the same convention.
- `NEXT_PUBLIC_*` is build-time inlined — UI env changes require image rebuild. All pages use `NEXT_PUBLIC_API_BASE_URL` (split fixed 2026-07-21).
- E2E tests must self-provision data (fresh-volume safe).
- JSON: `JsonStringEnumConverter` + `UtcDateTimeJsonConverter` are registered globally (2026-07-21) — requests accept string or numeric enums; date-only strings ("2026-07-08") are treated as UTC (Npgsql timestamptz rejects Unspecified).
- Old browser tokens (pre-RBAC, no permission claims) get 403 on all `[HasPermission]` endpoints — log out/in.
- Uploads: storage-service accepts only PDF/JPEG/PNG/DOC/XLS ≤10MB despite API allowing more — imaging uploads 500 (unfixed).
- AI/RAG endpoints need an LLM ApiKey in appsettings or they 500.

## Backlog (prioritized)

1. Multi-branch Stages 2–4 (active).
2. ~~Cabin E2E coverage~~ done 2026-07-22 (tier4, incl. employees + branches).
3. UI-level E2E for employees page + bed-board cabins (api layer covered; browser flows pending).
4. Storage/API upload-policy mismatch (GIF/DICOM/size).
5. P3 tenant-custom permission bundles — deferred (Designation covers titles; revisit only on real demand).
6. Admin dashboard KPI placeholders; dead login-page links.
