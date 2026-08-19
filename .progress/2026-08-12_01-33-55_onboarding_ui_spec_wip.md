# 2026-08-12 — Fully-visible onboarding UI spec + 3 real UI bugs fixed (in progress)

## What was done

Goal: the classic `onboarding-story.spec.ts` demo only shows steps 1/4/5 in the browser
(sales pipeline + provisioning run via API). Built `tests/e2e/onboarding-story-ui.spec.ts`
in mediconyx-e2e: **every step through the browser** — contact form → sales login →
claim/qualify → hospital profile → technical → modules → 3 document uploads → pricing →
approve → confirm payment → provision → MailHog → first login.

While smoke-testing it, found and fixed **3 genuine UI bugs** (all in mediconyx-ui):

1. `components/sales/HospitalProfileForm.tsx` — posted `""` for optional fields;
   API `[Url]`/`[EmailAddress]` reject empty strings → profile save always failed
   unless every optional field was filled. Now sends nulls.
2. `components/sales/RequestDetail.tsx` — document upload read
   `localStorage.getItem("accessToken")` (always null; zustand persists as
   `auth-storage`) → every sales document upload 401'd. Now uses
   `useAuthStore.getState().accessToken`.
3. `components/patient/PatientReportUpload.tsx` — same null-token bug on patient
   report uploads. Same fix.

Infra: Windows Hyper-V stole port 8026 (exclusion range 7960–8059). Test MailHog
moved to **8126**: `docker-compose.test.yml`, both onboarding specs' fallback,
and `AGENTS.md` updated.

E2E additions: `tests/fixtures/{license,tax-certificate,business-registration}.pdf`
(minimal valid PDFs for the 3 mandatory document uploads).

## State (session paused — PC shutdown)

- Test stack was up; MailHog healthy on 8126.
- test-ui **successfully rebuilt** at the end of the session (verified: served chunk
  contains `getState().accessToken`, UI 200). Earlier "rebuild" had silently failed
  (wrong cwd: compose file is in repo root, command ran from mediconyx-ui).
- Smoke runs 1–5 iterated: label-selector bug fixed (exact `normalize-space` match),
  success-banner assertions replaced (onSaved reload unmounts them — profile save is
  asserted via the Approve button appearing = Qualified→InProgress transition).
- Upload step not yet verified with the new bundle — smoke run #6 is the first thing tomorrow.

## Tomorrow: resume here

1. Stack: `docker compose -f docker-compose.test.yml up -d` (test-ui already has all fixes).
2. Smoke: `cd ../mediconyx-e2e && TEST_ENV=test npx playwright test tests/e2e/onboarding-story-ui.spec.ts --workers=1`
3. Demo: `TEST_ENV=test SLOW_MO=2000 npx playwright test tests/e2e/onboarding-story-ui.spec.ts --headed --workers=1 && node scripts/collect-videos.mjs onboarding-story-ui-slow`
4. Consider an npm script (e.g. `test:onboarding:ui`) for the new spec.
5. **Uncommitted work piling up** (2 sessions): Stage 3 API changes (8/5), today's UI
   fixes + spec + fixtures + MailHog port move + `.gitignore` (.qwen). Nothing committed yet.
