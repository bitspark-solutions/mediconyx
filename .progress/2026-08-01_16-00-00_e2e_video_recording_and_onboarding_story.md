# 2026-08-01 — E2E Video Recording (MP4) + Onboarding Story Test

## What was built

- **Video on for browser tests**: `playwright.config.ts` now records `video: 'on'` for the `ui` and `e2e` projects (the `api` project has no browser to record).
- **New spec** `tests/e2e/onboarding-story.spec.ts` — the complete story in one visible flow: hospital submits contact form (browser) → sales claims/qualifies/profile/modules/pricing/paid (API) → master provisions (API) → welcome email shown in MailHog (browser) → new hospital admin's first login (browser, temp password scraped from the email).
- **Collector** `scripts/collect-videos.mjs` — walks `test-results/**/video.webm`, converts each to MP4 with ffmpeg (H.264, faststart; falls back to copying .webm if ffmpeg is missing) into `test-videos/<YYYY-MM-DD_HH-mm-ss>_<short-description>/`.
- **npm scripts**: `npm run test:onboarding` (story + collect as `..._onboarding-story`), `npm run test:env:video` (full suite + collect as `..._e2e-run` or pass a description).
- `test-videos/` added to `.gitignore` (also fixed a mangled last line in that file).

## Verified
- `npm run test:onboarding` → story passes (11.9s video, MP4 verified with ffprobe) → `test-videos/2026-08-01_15-46-04_onboarding-story/`
- Full suite: **153 passed / 0 failed / 3 skipped**; 54 MP4s → `test-videos/2026-08-01_15-54-32_full-suite/`

## Bugs fixed along the way
- `contact-sales-flow.spec.ts` flaked when faker produced `'&<>"` in hospital names (API HTML-encodes them → exact-match assertions failed). Exported `sanitize()` from data-factory and applied it to all three tests in that spec (the earlier fix had only covered one).
- Docker Desktop was down mid-session; test stack recreated on fresh tmpfs (self-healing seeders proved again).

## Note
- Full-suite runs now produce ~20 flow videos (~150KB each) — trim `test-results/` periodically; `test-videos/` is gitignored.

## 2026-08-02 follow-up: video tuning + a real startup bug fixed

- **Short videos explained**: 1s clips were not corruption — ui-project micro-tests ("is badge visible?") genuinely finish in ~1s. Config changed: `ui` project → `retain-on-failure` (video kept only when a micro-test breaks), `e2e` project → `on` (all flows recorded). A full run now yields ~20 meaningful flow videos instead of 54 mixed ones.
- **Real bug found via the video run**: after a Docker Desktop restart, test-api booted *before* Postgres finished initializing → `MigrateAsync` failed with connection-refused → the failure was **swallowed** (logged only) → API served requests against an EMPTY schema → 105/146 tests failed with "relation does not exist". Fixed in `Data/Seeding/DatabaseInitializer.cs`: `MigrateWithRetryAsync` — 10 attempts, 3s backoff. This also de-fangs the long-standing "migration errors swallowed at startup" debt item for the db-not-ready case.
- Verified: full suite green again (**151 passed / 0 failed**) with `test-videos/2026-08-02_12-58-54_full-suite-final/` (20 flow videos).
- Ops note: Docker Desktop stopped twice mid-session (idle/reboot) — the retry now covers API boot ordering, but if tests fail with connection errors, check `docker ps` first.
