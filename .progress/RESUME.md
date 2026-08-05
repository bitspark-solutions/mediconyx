# RESUME — Mediconyx Current State

> Stable resume point. **Update this file at the end of every work session.**
> Session history lives in `.progress/<datetime>_<title>.md`; plans in `.plan/`.
> New sessions: read this file first (the project `AGENTS.md` instructs this).
> Last updated: 2026-08-05 — Multi-branch Stage 3 (flows) done.

## Where we are

Story coverage: master admin ✅, sales→approval→provisioning ✅, hospital profile/logo ✅, employee onboarding + custom designations + multi-role ✅ (NEW), doctor-as-staff duality ✅ (NEW `attach-profile`), IPD wards/beds/admissions/ICU/PICU/daycare ✅, cabins ✅ (NEW), per-bed-day billing ✅.
Big refactor done: `Program.cs` 14 lines, DbContext 70 lines + `Data/Configurations/`, seeders in `Data/Seeding/`.
E2E: **152 passed / 0 failed / 4 skipped** (156 total) — last full green run 2026-08-05. Browser tests record video (`e2e` project always, `ui` project on failure); collect MP4s with `node scripts/collect-videos.mjs <desc>` into `test-videos/<datetime>_<desc>/`. Onboarding story: `npm run test:onboarding`.

## Stacks

- **Dev** (docker-compose.yml): UI http://localhost:9673, API http://localhost:9765 (+/swagger), DB 9543, storage 7890, MailHog 8025. Rebuilt with all changes 2026-08-05 (Stage 3 migration backfilled dev admissions from their beds).
- **Test** (docker-compose.test.yml): UI 9674, API 9766, storage 7891. Fresh-volume, all changes, E2E green.
- Run E2E: `cd ../mediconyx-e2e && npm run test:env`.
- Logins: hospital admin `admin@citygeneral.local`/`Hospital@123`, master `master@mediconyx.local`/`MasterPass123!` (dev) / `Master@123` (test). Full list in `readme.md`.

## Active workstream: multi-branch (Max Healthcare model)

Plan: `.plan/2026-07-21_multibranch_support.md`. Progress logs: `.progress/2026-07-21_23-55-00_multibranch_stage1_foundation.md`.

- ✅ **Stage 1** — Branch entity, `/api/branch` CRUD, migration `20260721000200_AddBranches` with Main-Branch backfill. Verified live.
- ✅ **Stage 2 — IPD scoping** — `BranchId` on Ward/Bed, per-branch ward-code uniqueness, `?branchId=` filters, bed-board branch filter, `/portal/admin/branches` UI. Also: `BranchSeeder` heals fresh-DB tenants; provisioning creates MAIN. E2E 152/152 green (2026-07-22).
- ✅ **Stage 3 — Flows** (2026-08-05) — `BranchId` on Appointment/Admission/Invoice (migration `20260805000100_AddBranchIdToFlowsAndUsers`, admissions backfilled from their bed, rest → Main); `PrimaryBranchId` on User + JWT `branch_id` claim; `?branchId=` filters + create-time branch validation (defaults to Main); employees UI branch field. Unit tests 176 passed/1 skipped; E2E green.
- ⬜ **Stage 4 — Polish** (RESUME HERE): header branch switcher, role-aware default scoping (`branch_id` claim → default filter for non-admin staff), cross-branch isolation E2E, seed demo 2nd branch.

## Standing decisions & gotchas

- One class per file (hard rule). No AgentSwarm. Check `usage` before tasks; stop at 95%.
- Hand-written migrations MUST carry `[DbContext(typeof(ApplicationDbContext))]` + `[Migration("id")]` or EF silently ignores them.
- `DatabaseInitializer.MigrateWithRetryAsync` (2026-08-02): 10 attempts × 3s — covers Postgres-not-ready at API boot (Docker daemon restarts ignore compose healthchecks).
- Keep ModelSnapshot in sync: scaffold throwaway migration → delete its files → snapshot stays regenerated. Verify with `dotnet ef migrations has-pending-model-changes`.
- Docker Desktop restart can leave container host-port bindings dead while the container shows "Up" — `docker restart <container>` rebinds (bit us on test MailHog 8026, 2026-08-05).
- Tenant isolation is manual `.Where(TenantId == ...)`; BranchId filtering must follow the same convention.
- `NEXT_PUBLIC_*` is build-time inlined — UI env changes require image rebuild. All pages use `NEXT_PUBLIC_API_BASE_URL` (split fixed 2026-07-21).
- E2E tests must self-provision data (fresh-volume safe).
- JSON: `JsonStringEnumConverter` + `UtcDateTimeJsonConverter` are registered globally (2026-07-21) — requests accept string or numeric enums; date-only strings ("2026-07-08") are treated as UTC (Npgsql timestamptz rejects Unspecified).
- Old browser tokens (pre-RBAC, no permission claims) get 403 on all `[HasPermission]` endpoints — log out/in.
- Uploads: unified policy PDF/JPEG/PNG/GIF/DICOM ≤50MB both sides (2026-07-25); storage requires X-Api-Key (dev: empty = open with warning); soft-deletes purged after 30d by SoftDeletePurgeService; signed download URLs are real HMAC tokens. Storage host ports are 8890/8891 (Windows reserves 7871–7970).
- Kebab-case routes need explicit `[Route]` — `[controller]` does NOT map dashes (fixed for file-attachment, vital-sign, medical-note, doctor-schedule on 2026-07-25).
- AI/RAG endpoints need an LLM ApiKey in appsettings or they 500.

## Backlog (prioritized)

1. Multi-branch Stages 3–4 (active).
2. ~~Cabin E2E coverage~~ done 2026-07-22 (tier4, incl. employees + branches).
3. UI-level E2E for employees page + bed-board cabins (api layer covered; browser flows pending).
4. ~~Storage/API upload-policy mismatch~~ done 2026-07-25 (+ storage API-key auth, purge job, real signed URLs, kebab routes, AddAiRagTables migration).
5. E2E for file upload/download (chain now works; no test coverage yet — high value, it hid 2 bugs).
6. P3 tenant-custom permission bundles — deferred (Designation covers titles; revisit only on real demand).
7. Admin dashboard KPI placeholders; dead login-page links; owner dashboard with real metrics + master UI for subscription edits (API exists: `PUT /api/TenantSettings/subscription/{tenantId}`).
8. ~~Module toggles not enforced~~ done 2026-07-26 (editable + middleware gate). ~~Subscription management~~ done 2026-07-26 (master endpoint, seat limit, expiry login block).
