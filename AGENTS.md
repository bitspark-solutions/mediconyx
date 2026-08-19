# Mediconyx — Agent Guide

Multi-tenant hospital management SaaS: `mediconyx-api` (.NET 10, ASP.NET Core + EF Core + PostgreSQL), `mediconyx-ui` (Next.js 16 + React 19 + Tailwind 4 + Zustand), `storage-service` (.NET 8 minimal API), plus a sibling E2E repo at `../mediconyx-e2e` (Playwright).

## FIRST: read `.progress/RESUME.md`

At the start of every session, read **`.progress/RESUME.md`** before doing anything else. It holds the current state, the active workstream, the exact resume point, standing decisions, and the backlog. Update it at the end of every work session (it's the handover contract).

- Session logs: `.progress/<YYYY-MM-DD_HH-mm-ss>_<title>.md` — write one after any significant work.
- Plans: `.plan/<date>_<title>.md` — for anything multi-step or architectural.

## Build / run / test

- Dev stack: `docker compose up -d` → UI http://localhost:9673, API http://localhost:9765 (/swagger), DB 9543, storage 8890, MailHog 8025.
- Test stack: `docker compose -f docker-compose.test.yml up -d` → UI 9674, API 9766, storage 8891, MailHog 8126 (moved off 8026 on 2026-08-12 — Windows Hyper-V exclusion range 7960–8059 stole it).
- API build: `"C:/Program Files/dotnet/dotnet.exe" build mediconyx-api/mediconyx-api.csproj`
- API tests: `"C:/Program Files/dotnet/dotnet.exe" test mediconyx-api/Tests/mediconyx-api.Tests.csproj` (expect 170 passed / 1 skipped)
- UI check: `cd mediconyx-ui && npx tsc --noEmit`
- E2E: `cd ../mediconyx-e2e && npm run test:env` (test stack must be up; expect 151+ passed / 0 failed). Onboarding demo video: `npm run test:onboarding:slow` (headed, slow-mo) — videos land in `mediconyx-e2e/test-videos/<datetime>_<desc>/`.
- Login matrix (seeded): see `readme.md`. Test-stack master password is `Master@123`, dev is `MasterPass123!`.

## Hard conventions

- **One class per file.** No exceptions — every model, DTO, enum, helper gets its own file.
- **Tenant isolation is manual**: every query filters `.Where(x => x.TenantId == tenantId)`; branch filtering (Stage 2+) follows the same pattern. No EF global filters exist.
- Hand-written migrations must carry `[DbContext(typeof(ApplicationDbContext))]` + `[Migration("<id>")]` attributes or EF silently ignores them. After model changes, sync the snapshot: scaffold a throwaway migration, delete its files, verify with `dotnet ef migrations has-pending-model-changes`.
- Kebab-case API routes need explicit `[Route("api/kebab-name")]` — `[controller]` does not map dashes.
- JSON requests accept string or numeric enums globally; date-only strings are treated as UTC (`JsonStringEnumConverter` + `UtcDateTimeJsonConverter` registered in `Extensions/ServiceCollectionExtensions.cs`).
- E2E tests must self-provision their data (fresh-volume safe) and keep codes ≤20 chars.
- Don't commit secrets; `.env` is local-only. Demo passwords in seeders are dev-only.

## Environment notes (Windows)

- Docker Desktop may be stopped — `docker ps` first; API boot retries migrations (10×3s) if Postgres isn't ready.
- Windows reserves ports 7871–7970 (Hyper-V) — storage host ports are 8890/8891 because of this.
