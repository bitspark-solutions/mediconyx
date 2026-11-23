# Session — Stack up, stale images found, MailHog ports moved, full suite green

Date: 2026-11-24

## Goal
Start the project (Docker was stopped) and verify the November work — especially the
2026-11-17 `new-consultation-flow` browser fix that had never been confirmed against a live stack.

## Findings

1. **Docker Desktop was stopped**; engine came up cleanly (v29.9.2). Both dev and test stacks started.
2. **UI images were stale**:
   - `mediconyx-test-ui` built 2026-08-12 (3 months old — predates both the Nov 9 consultation work and the Nov 17 fix).
   - `mediconyx-ui` built 2026-08-05 (predates multi-branch Stage 4 branch switcher).
   - API/storage images were current (2026-11-17). Rebuilt both UI images from HEAD.
3. **MailHog host ports had drifted into a new Windows exclusion range**:
   `netsh int ipv4 show excludedportrange protocol=tcp` now reserves **8067–8566**, so 8125/8126
   failed with "socket access permissions forbidden" (container showed Up but with no published port).
   - Moved dev MailHog web UI 8125 → **9125** (SMTP stays 2525).
   - Moved test MailHog web UI 8126 → **9126** (SMTP 1026 unchanged).
   - Updated: `docker-compose.yml`, `docker-compose.test.yml`, `mediconyx-e2e` onboarding specs
     (both had stale 8025/8126 defaults), `AGENTS.md`, `RESUME.md`, `docs/wiki/Home.md`.

## Verification (test stack, fresh UI images)

| Suite | Result |
|---|---|
| .NET unit/integration | **176 passed / 1 skipped / 0 failed** |
| E2E API project | **105 passed / 3 skipped / 0 failed** |
| E2E browser project | **22 passed / 0 failed** — incl. `new-consultation-flow` ✅ and both onboarding-story suites |
| E2E UI pages project | **33 passed / 1 skipped** |
| E2E total | **160 passed / 4 skipped / 0 failed** (164) |

Auth smoke-tested on both stacks (master login OK on 9765 and 9766); dev UI serves HTTP 200.

## Notes / gotchas
- A stale container that shows "Up" but has no `docker port` mapping may need `--force-recreate`,
  not just `docker restart`, if the host port became forbidden after a reboot.
- Check `netsh int ipv4 show excludedportrange protocol=tcp` before choosing new host ports —
  the exclusions have now moved twice (7960–8059 → 8067–8566).
- The previously open item (browser E2E failure from 2026-11-17) is now **closed**: it was a
  combination of the stale test-ui image and the dead MailHog binding.

## Follow-ups (unchanged backlog)
- Cross-branch isolation E2E specs still not written.
- File upload/download E2E coverage still missing.
- Wiki/graphify artifacts are from 2026-11-01 (commit `6b0a259f`) — regenerate after more changes.
- `POST /api/Doctor` seat-limit bypass still undecided.

## Same session — Compose consolidation (dev + test + prod in one file)

- **`docker-compose.test.yml` deleted.** `docker-compose.yml` is now the single stack;
  services are selected with Compose profiles via per-environment env files:
  - dev: `docker compose up -d` (`.env`, `COMPOSE_PROFILES=dev`)
  - test: `docker compose --env-file .env.test up -d` (`COMPOSE_PROFILES=test`)
  - prod: `docker compose --env-file .env.prod up -d` (`COMPOSE_PROFILES=prod`)
  - containerized E2E: `--env-file .env.test --profile test --profile e2e`
    (note: `--profile` replaces `COMPOSE_PROFILES`, so both profiles must be listed)
- Everything is env-driven: host ports, bind IPs, credentials, SMTP (`SMTP_*`),
  public URLs, `ASPNETCORE_ENVIRONMENT`. Dev defaults are inline so `.env` only needs
  secrets + `COMPOSE_PROFILES=dev`.
- **Prod** = `prod` profile: MailHog is not started (real SMTP), DB and storage bound
  to `127.0.0.1`. Full template in `.env.prod.example`; `.env.prod` is gitignored.
- **UI build arg bug fixed:** `NEXT_PUBLIC_API_BASE_URL` is now passed as a build arg in
  the dev/prod paths too (previously only the test file did it, so prod URLs never
  reached the bundle).
- Each environment is its own Compose project (`COMPOSE_PROJECT_NAME`), so stacks
  coexist. Dev keeps project `mediconyx` → existing `mediconyx_db_data` volume reused.
- Added `.env.example`; updated `Makefile` (`up-test` / `down-test` / `up-prod` / `down-prod`),
  `AGENTS.md`, `README.md`, wiki `Architecture.md` + `Testing.md`, `RESUME.md`.
- Verified: both stacks up under the new file, master login OK on 9765/9766, all
  published URLs return 200, full E2E re-run against the new test stack.
## Same session — post-consolidation E2E (final)

- **Full E2E `npm run test:env` on the new test stack: green — 161 passed / 0 failed, exit code 0.** All API, browser (incl. new-consultation-flow), and UI page suites pass on the first full run under the single compose file.
- The prior 50-test "failure" batch was caused by the test UI being intermittently unreachable during the earlier run (host port 9674 was timing out while the UI was mid-restart); after `docker restart` + `--force-recreate`, localhost:9674 returns 200 reliably and all green.

### Changes in this session (one commit per repo, ready to commit)

**mediconyx (dev repo):**
- Rewrote `docker-compose.yml` → single profile-based file (dev/test/prod + optional `--profile e2e` runner); deleted `docker-compose.test.yml`
- Added `.env.test` (committed test creds), `.env.prod.example` (template), `.env.example` (dev template)
- `.env`: added `COMPOSE_PROFILES=dev`; `.gitignore`: added `.env.prod`
- `Makefile`: `up-test`/`down-test`/`up-prod`/`down-prod`, `SERVICES` includes storage
- `AGENTS.md`, `README.md`, wiki `Architecture.md` + `Testing.md`, `RESUME.md`, session log
- `mediconyx-api/Middleware/GlobalExceptionHandlerMiddleware.cs`: `HasStarted` early-return so real exceptions aren't masked
- Both API images rebuilt

**mediconyx-e2e (sister repo):**
- `tests/api/tier2-ipd.spec.ts`: bed test uses its own `ensureWard()` instead of `listWards()[0]` (fixed the FK-23503 / 500 race)
- Both onboarding specs: MailHog default 9125/9126
