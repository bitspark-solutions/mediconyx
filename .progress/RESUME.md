# RESUME — Mediconyx Current State

> Stable resume point. **Update this file at the end of every work session.**
> Session history lives in `.progress/<datetime>_<title>.md`; plans in `.plan/`.
> New sessions: read this file first (the project `AGENTS.md` instructs this).
> Last updated: 2026-11-09 — NewConsultationWorkflow fully functional (real appointments) + new E2E suites; 2 backend bugs fixed (patient-number race, query-date UTC); full E2E 161/0/3 green.
 
 ## Where we are
 
 Story coverage: master admin ✅, sales→approval→provisioning ✅, hospital profile/logo ✅, employee onboarding + custom designations + multi-role ✅, doctor-as-staff duality ✅ (`attach-profile`), IPD wards/beds/admissions/ICU/PICU/daycare ✅, cabins ✅, per-bed-day billing ✅, fully-visible browser onboarding story ✅.
 Big refactor done: `Program.cs` 14 lines, DbContext 70 lines + `Data/Configurations/`, seeders in `Data/Seeding/`.
 E2E: **161 passed / 0 failed / 3 skipped** (164 total) — 100% green run 2026-11-09. Browser tests record video (`e2e` project always, `ui` project on failure); collect MP4s with `node scripts/collect-videos.mjs <desc>` into `test-videos/<datetime>_<desc>/`. Onboarding story: `npm run test:onboarding` (API-driven) and `npm run test:onboarding:ui` / `npm run test:onboarding:ui:slow` (fully-visible browser-driven).
 
 ## Stacks
 
 - **Dev** (docker-compose.yml): UI http://localhost:9673, API http://localhost:9765 (+/swagger), DB 9543, storage 8890, MailHog **8125** (web) / 2525 (SMTP) — moved off 8025 on 2026-11-01, Hyper-V exclusion 7937–8036. Rebuilt with all changes 2026-08-05 (Stage 3 migration backfilled dev admissions from their beds).
 - **Test** (docker-compose.test.yml): UI 9674, API 9766, storage 8891, MailHog 8126. Fresh-volume, all changes, E2E green.
 - Run E2E: `cd ../mediconyx-e2e && npm run test:env`.
 - Logins: hospital admin `admin@citygeneral.local`/`Hospital@123`, master `master@mediconyx.local`/`MasterPass123!` (dev) / `Master@123` (test). Full list in `readme.md`.
 
 ## Active workstream: multi-branch (Max Healthcare model)
 
 Plan: `.plan/2026-07-21_multibranch_support.md`. Progress logs: `.progress/2026-07-21_23-55-00_multibranch_stage1_foundation.md`.
 
 - ✅ **Stage 1** — Branch entity, `/api/branch` CRUD, migration `20260721000200_AddBranches` with Main-Branch backfill. Verified live.
 - ✅ **Stage 2 — IPD scoping** — `BranchId` on Ward/Bed, per-branch ward-code uniqueness, `?branchId=` filters, bed-board branch filter, `/portal/admin/branches` UI. Also: `BranchSeeder` heals fresh-DB tenants; provisioning creates MAIN. E2E 152/152 green (2026-07-22).
 - ✅ **Stage 3 — Flows** (2026-08-05) — `BranchId` on Appointment/Admission/Invoice (migration `20260805000100_AddBranchIdToFlowsAndUsers`, admissions backfilled from their bed, rest → Main); `PrimaryBranchId` on User + JWT `branch_id` claim; `?branchId=` filters + create-time branch validation (defaults to Main); employees UI branch field. Unit tests 176 passed/1 skipped; E2E green.
 - ⬜ **Stage 4 — Polish** (next): header branch switcher, role-aware default scoping (`branch_id` claim → default filter for non-admin staff), cross-branch isolation E2E, seed demo 2nd branch.
 
 ## Completed: fully-visible onboarding UI spec & E2E verification (2026-08-29)
 
 Details: `.progress/2026-08-29_00-42-00_onboarding_ui_story_complete.md`.
 1. Fixed duplicate fixture SHA-256 collision where `license.pdf`, `tax-certificate.pdf`, and `business-registration.pdf` had identical contents and the API deduplication discarded the 2nd and 3rd uploads.
 2. Fixed `uiLogin` to clear previous sessions so `AuthRedirectGuard` does not bounce back to sales portal before logging in as newly provisioned hospital admin.
 3. Added `npm run test:onboarding:ui` and `npm run test:onboarding:ui:slow` in `mediconyx-e2e`. Generated full slow-mo MP4 demo video in `test-videos/2026-08-29_00-41-28_onboarding-story-ui-slow`. All 153 tests passed (0 failed).

## Completed: NewConsultationWorkflow functional + E2E (2026-11-09)

Details: `.progress/2026-11-09_18-24-00_new_consultation_workflow_fixed.md`. Plan: `.plan/2026-11-09_new_consultation_workflow.md`.
The consultation step now creates a **real appointment** (patient register → department/doctor/slots → `POST /api/appointment`; `doctorId` = User id; slots endpoint with manual-time fallback since demo seeders create no schedules). Two backend bugs fixed: patient-number unique-violation race (retry ≤5× in `PatientService`) and query-string dates binding `Kind=Unspecified` → 400 (`UtcDateTimeModelBinder`, registered first in `ModelBinderProviders`). UI rot fixed: doctor-portal cancel PATCH→POST, receptionist check-in body raw `"CheckedIn"`, cancel payload `reason`. New E2E: `consultation-flow.spec.ts` (API, 6) + `new-consultation-flow.spec.ts` (browser). Tier4 flakiness root-caused to demo-tenant seat limit (50) vs accumulated test users (54) — spec now raises licenses to 1000 via master `PUT /api/TenantSettings/subscription/2`. Videos: `test-videos/2026-11-09_18-24-03_new-consultation`.

## Tooling: graphify code-graph MCP (2026-09-29)

Installed `graphify-local-server` at `C:\Users\Mohsin\Projects\graphify-local-server`, running as the **`graphify-local-server` Docker container** (`restart: unless-stopped`, port 28848). Registered as MCP in Mavis, Antigravity, Qoder, VS Code — all four use the `docker exec -i graphify-local-server python /app/mcp/graphify_mcp.py` form. Tools take `project_path: "mediconyx"`.

Mediconyx graph: 5097 nodes / 9639 edges / 287 communities, built inside the container from commit `83c5cf58` (master). Artifacts land on the host via the bind mount at `data/mediconyx/graphify-out/` (`graph.json`, `graph.html`, `GRAPH_REPORT.md`).

Gotchas:
- Build/refresh over REST (`POST /api/build` with `path: "/app/workspaces/mediconyx"`, `force: true`, ~4 min). The `graphify_build` **MCP tool times out** on a full rebuild — drive it over REST instead.
- Graphs must be built with **container** paths (`/app/workspaces/...`, set via `HOST_WORKSPACE_DIR` in the repo `.env`). A host-path build leaves file references that don't resolve inside the container.
- After a build, run `docker exec graphify-local-server graphify cluster-only /app/data/mediconyx` to emit `GRAPH_REPORT.md`. Community *names* stay as `Community N` placeholders unless an LLM backend key (e.g. `GOOGLE_API_KEY`) is set — the structural edges are still real.
- The leftover native `.venv` in that repo is unused; safe to delete.
- On Git Bash, docker-exec graphify commands need `MSYS_NO_PATHCONV=1` or `/app/...` paths get mangled.
- Rebuilt 2026-11-01 from commit `6b0a259f`: 5231 nodes / 9827 edges / 316 communities. Project wiki generated at `docs/wiki/` (Home.md index + Graph-Index.md mapping subsystems → community IDs).

## Vulnerability remediation (2026-11-10) — ABSOLUTE 0 VUL across all 4 projects

- **mediconyx-api (.NET 10)**: `System.Security.Cryptography.Pkcs` 9.0.0 had GHSA-2fmv-rv2c-59x7 (moderate). Pinned to `10.0.14` in `mediconyx-api.csproj` → `dotnet list package --vulnerable` clean; `dotnet build` 0 warnings/0 errors.
- **storage-service (.NET 8)**: already 0 VUL; `dotnet build` 0 warnings/0 errors.
- **mediconyx-ui (Next.js 16)**: 5 prod (1 critical `next`, 4 high) + 16 dev → `npm audit fix --force` ×2 → `next` 16.4.5, `eslint-config-next` 15.5.29, `postcss`, `sharp`, `source-map-js`, `nanoid`, etc. → `npm audit` = 0; `npx tsc --noEmit` clean.
- **mediconyx-e2e (Playwright)**: 1 high dev (`@faker-js/faker`) → `npm audit fix` → `npm audit` = 0.

Rule respected: only `package.json` modified via `npm audit fix`; no `package-lock.json` hand-edited.

## Standing decisions & gotchas

- One class per file (hard rule). No AgentSwarm. Check `usage` before tasks; stop at 95%.
- Hand-written migrations MUST carry `[DbContext(typeof(ApplicationDbContext))]` + `[Migration("id")]` or EF silently ignores them.
- `DatabaseInitializer.MigrateWithRetryAsync` (2026-08-02): 10 attempts × 3s — covers Postgres-not-ready at API boot (Docker daemon restarts ignore compose healthchecks).
- Keep ModelSnapshot in sync: scaffold throwaway migration → delete its files → snapshot stays regenerated. Verify with `dotnet ef migrations has-pending-model-changes`.
- Docker Desktop restart can leave container host-port bindings dead while the container shows "Up" — `docker restart <container>` rebinds (bit us on test MailHog 8026, 2026-08-05).
- Tenant isolation is manual `.Where(TenantId == ...)`; BranchId filtering must follow the same convention.
- `NEXT_PUBLIC_*` is build-time inlined — UI env changes require image rebuild. All pages use `NEXT_PUBLIC_API_BASE_URL` (split fixed 2026-07-21).
- JSON: `JsonStringEnumConverter` + `UtcDateTimeJsonConverter` are registered globally (2026-07-21) — requests accept string or numeric enums; date-only strings ("2026-07-08") are treated as UTC (Npgsql timestamptz rejects Unspecified). Since 2026-11-09 the same UTC rule applies to **query/route**-bound DateTimes via `UtcDateTimeModelBinder` (bodies go through the JSON converter; queries needed their own binder).
- E2E tests must self-provision data (fresh-volume safe). The test DB volume is never wiped — data accumulates across runs (unique suffixes keep creates safe). Tier4 flakiness lesson (2026-11-09): demo tenant `UserLicenses=50` + accumulated users → `createUser` 400s; specs needing users raise seats via master-only `PUT /api/TenantSettings/subscription/{id}`.
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
9. `POST /api/Doctor` bypasses the tenant seat limit (`UserService.CreateUserAsync` enforces it; doctor creation adds the user directly) — decide intended behavior and align (2026-11-09).
