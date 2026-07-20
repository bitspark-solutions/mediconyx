# 2026-07-21 — Environment Setup, Usage Monitoring & Project Onboarding

## What was done

### Terminal usage commands (outside project, affects both PowerShell profiles + Git Bash)
- Added `usage` command (`Get-AiUsage`) that reports **used percentage** for:
  - Kimi for Coding — weekly quota + 5h window, via `GET https://api.kimi.com/coding/v1/usages` (Bearer = `sk-kimi-` subscription key; endpoint reverse-engineered from `@moonshot-ai/kimi-code` 0.28.1, `packages/oauth/src/managed-usage.ts`).
  - MiniMax Token Plan — 5h + weekly, via `GET https://www.minimax.io/v1/token_plan/remains` (documented endpoint, `sk-cp-` key).
- Installed in: `Documents/PowerShell/Microsoft.PowerShell_profile.ps1` (pwsh 7), `Documents/WindowsPowerShell/Microsoft.PowerShell_profile.ps1` (5.1), `~/.bashrc` (Git Bash).
- Added `source` command (global function wrapping `. $PROFILE`) to both PowerShell profiles; made `Get-AiUsage`/`usage` global-scoped so they survive profile reloads.
- Fixed 5.1 profile startup errors: PSReadLine prediction options (`-PredictionSource`/`-PredictionViewStyle`) now wrapped in `try/catch` (5.1 may load PSReadLine 2.0.0 which lacks them, or 2.4.5 which fails on redirected consoles).

### Project exploration (no code changes)
- Built a full mental model of all 4 components: `mediconyx-api` (.NET 10, not 8 as docs claim), `mediconyx-ui` (Next.js 16 + React 19, not 14), `storage-service` (.NET 8, no auth), `mediconyx-e2e` (78 Playwright tests, 3 layers).
- Last E2E run (Jul 21): all 78 failed, but root cause is environmental — test stack (`docker-compose.test.yml`: API 9766, UI 9674) was not running. No test-logic failures observed.

### Key findings / known issues (not yet fixed)
- Tenant isolation is manual (`.Where(TenantId == ...)`) everywhere — no EF global filters; biggest correctness risk.
- "RAG" is prompt-stuffing, not vector search: last 20 notes/10 Rx/10 vitals/10 files with hardcoded scores; pgvector unused.
- UI env-var split bug: `register`, `verify-email`, `doctors` pages read `NEXT_PUBLIC_API_URL`; everything else uses `NEXT_PUBLIC_API_BASE_URL` — breaks on test stack (9766).
- Upload policy mismatch: API allows GIF/DICOM/50MB, storage-service only PDF/JPEG/PNG/DOC/XLS ≤10MB → imaging uploads 500.
- Startup swallows migration errors; secrets committed in `appsettings.json`; storage-service has no auth and unbounded soft-delete growth.
- E2E: Playwright image pinned 1.52 vs installed 1.59; shared-state race on tenant-settings description.

### Standing rules (persisted globally in `~/.kimi-code/AGENTS.md`)
- Check `usage` before every task / long command run; stop at ≥95% on any window and write a `.progress/` worklog.
- No AgentSwarm; direct tools only.
- Worklogs in `.progress/` as `<datetime>_<title>.md`.

## Current state
- Usage at time of writing: Kimi weekly 5%, 5h 22%; MiniMax 5h 15%, weekly 15%.
- No project code modified in this session.

## Next steps (candidates)
- Bring up test stack (`docker compose -f docker-compose.test.yml up -d`) and get a green E2E baseline (`npm run test:env` from `../mediconyx-e2e`).
- Fix the `NEXT_PUBLIC_API_URL` vs `NEXT_PUBLIC_API_BASE_URL` inconsistency.
- Align upload validation between API and storage-service.
