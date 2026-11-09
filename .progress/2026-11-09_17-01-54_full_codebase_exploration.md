# Session Log — Full Codebase Exploration (map built)

**Date:** 2026-11-09 17:01 · **Repo state:** `master` @ `6b0a259`, clean, synced with origin.
**Purpose:** Deep full-stack exploration pass — no code changes. Produced the complete architectural map below (also delivered in chat).

## What was explored
- Top-level: `README.md`, `PROJECT_OVERVIEW.md`, `Makefile`, both compose files, `AGENTS.md`, `.plan/` (14 files), `.progress/` (24 logs), `design-preview/`, agent-tool configs, git history (81 commits since 2025-11-12).
- `mediconyx-api`: all 30 controllers inventoried (routes+verbs), 38 entities + 42 configurations, 22 migrations, 5 seeders, auth/RBAC/module-middleware, ~55 services, test suite (166 methods, EF InMemory).
- `mediconyx-ui`: full route map (24 routes), single Zustand store, apiClient refresh flow, component inventory, dual-theme styling, Dockerfile.
- `storage-service`: minimal-API endpoints, X-Api-Key + HMAC signed URLs, sidecar metadata (no DB), purge BackgroundService.
- `mediconyx-e2e` (sibling): 3 Playwright projects, 14 specs, helper/POM inventory, video collection.

## Key facts confirmed (delta vs RESUME)
- RESUME's "170 tests" drift: actual suite is **166 test methods** (165 Fact + 1 Theory, 1 skipped) — readme says 176 passed; last recorded green run 2026-08-29 was 153/153 E2E. Numbers differ by source; re-verify at next test run.
- No background jobs in the API except seeders; **SoftDeletePurgeService (BackgroundService) lives in storage-service**, not the API.
- API has **no soft-delete columns** — deactivation is `IsActive` booleans; storage-service has its own `IsDeleted`+30d purge.
- HEAD (2026-10-16) is a 5-commit one-type-per-file refactor sweep; `ResendVerificationRequest.cs` sits in `Controllers/` (stray DTO).
- `mediconyx-ui/public/` contains stray dev notes with seeded credential info (`check_admin_details.txt`, `create_admin.txt`, …) — publicly served; hygiene item.
- Role→route map is duplicated in 4 UI places; `next.config.ts` and `next.config.js` both exist (both empty).

## No code changes made this session.
## Next up (unchanged): Multibranch Stage 4 — header branch switcher, role-aware default scoping, cross-branch isolation E2E, demo 2nd branch.
