# Portal E2E Fixes & Full Suite Green — Progress Report

**Date:** 2026-07-19
**Phase:** Test infrastructure hardening — portal E2E suite from 4 failures to 77/77 green
**Collaborators:** Mohsin Khan Shuvo + Kimi (continuing work started by a prior agent session: portal pages build-out + new portal test files)

---

## Summary

The prior agent built five portal pages (admin, doctor, nurse, patient, accountant — uncommitted) and authored a new 3-level Playwright suite in `mediconyx-e2e` (21 API tests passing), stopping just before fixing E2E strict-mode violations. This session took the suite across the finish line. Investigation of the 4 failing E2E tests revealed **2 real app bugs, 1 seed-data gap, 1 broken test-environment assumption, and 5 test-side defects** — all fixed and verified. The complete 3-level suite now runs **77 passed / 0 failed in 14.1s** against the test stack, and the .NET unit suite (158 tests) stayed green inside both Docker build gates.

The single most interesting root cause: the test stack was believed to have rate limiting disabled via `appsettings.Test.json`, but it never was — **.NET JSON configuration merges arrays by index, so `"GeneralRules": []` does NOT clear the base rules** from `appsettings.json`. The `100 req/min` wildcard rule kept applying and 429'd the parallel E2E run. Fixed in code by skipping `UseIpRateLimiting()` entirely in the Test environment.

---

## Files changed

### mediconyx (main repo)

| File | Status | Purpose |
|---|---|---|
| `mediconyx-api/Program.cs` | modified | (a) Idempotent Nurse + Accountant seed for city-general — placed **outside** the `hospitalTenant == null` block so existing databases get the new users; (b) `UseIpRateLimiting()` now skipped when `IsEnvironment("Test")` |
| `mediconyx-ui/app/portal/nurse/page.tsx` | modified | `htmlFor`/`id` pairs on Patient select + all 8 vitals inputs |
| `mediconyx-ui/app/portal/accountant/page.tsx` | modified | `htmlFor`/`id` pairs on Invoice, Amount, Method, Reference fields |
| `mediconyx-ui/app/portal/admin/page.tsx` | modified | `htmlFor`/`id` on the dynamic hospital-settings text fields (`settings-{key}`) |
| `mediconyx-ui/components/ProtectedRoute.tsx` | modified | Blocked-role path rendered `null` (permanent blank page if the redirect navigation is cancelled) — now renders an explicit "Redirecting…" state |
| `readme.md` | modified | Documented the 2 new seeded accounts |

(The five portal pages and the untracked `20260718000000_AddHospitalServicesTable` migration were pre-existing uncommitted work from the prior agent, not part of this session's delta.)

### mediconyx-e2e (test repo — prior agent's 4 new files, hardened here)

| File | Status | Purpose |
|---|---|---|
| `tests/helpers/portal-api-client.ts` | modified | Added `nurse`/`accountant` to `TEST_USERS`; fixed `recordPayment` route `/payment` → `/payments` (latent 404, matches `InvoiceController`) |
| `tests/pages/portals.page.ts` | modified | (a) `loginAs` **throws** when login fails instead of letting tests assert against the login page; (b) `vitalsTab` scoped to the nav list — strict-mode violation with the submit button of the same name; (c) `recordVitalsButton` disambiguated with `.last()`; (d) added `descriptionInput` locator |
| `tests/e2e/portals-flow.spec.ts` | modified | (a) Master password env-driven (`TEST_MASTER_PASSWORD`) — was hardcoded to the **dev** stack password, silently failing on the test stack; (b) admin settings test fills `descriptionInput` instead of a non-existent `textarea`; (c) wrong-role test: fixed 2s sleep → 15s condition polling |
| `tests/api/portals.spec.ts` | unchanged (prior agent) | 21 API portal tests — still all passing |

---

## Verification output

```text
=== New seed: nurse + accountant login on test stack (9766) ===
nurse login:      200 (JWT issued)
accountant login: 200 (JWT issued)
master login:     200 (Master@123 — test-stack password)

=== .NET build gate (runs inside Dockerfile, both rebuilds) ===
Passed! — 158 passed, 1 skipped, 0 failed

=== Level 2 — API suite ===
31 passed (3.4s)

=== Level 1+2+3 — full suite (npm run test:env) ===
77 passed (14.1s) — includes:
  ok Admin Portal E2E › admin views tenants, hospital settings (6.0s)
  ok Admin Portal E2E › master sees extra Users tab, regular admin does not (6.3s)
  ok Nurse Portal E2E › nurse views patient list and records vitals (5.4s)
  ok Receptionist Portal E2E › receptionist navigates portal sections (5.1s)
  ok Cross-portal › wrong role gets blocked from protected portal
  ok Contact Sales Full E2E Flow (all 4 tests)

=== Wrong-role block proven correct in isolation (debug browser script) ===
patient → /portal/doctor → t=2s URL=/unauthorized "Access Denied — Your role: Patient"
```

---

## Per-fix breakdown

### Fix 1: Missing Nurse/Accountant seed users (app bug — seed gap)

**Root cause:** The nurse E2E test logs in as `nurse@citygeneral.local` — no such user existed. Nurse and Accountant are first-class roles (`UserRole.Nurse = 3`, `UserRole.Accountant = 5`, used in controller authorization and portal guards) but were never seeded. The nurse portal additionally guards `allowedRoles={["Nurse"]}` — no substitute role could even stand in.

**Fix:** Idempotent seed block in `Program.cs`, keyed on `Subdomain == "city-general"`, **outside** the `if (hospitalTenant == null)` hospital seed. Critical placement: the hospital seed only runs on an empty database, so users added inside it would never reach existing dev/test databases.

### Fix 2: Rate limiting never actually disabled in Test env (broken assumption)

**Root cause:** `docker-compose.test.yml` comments claim `ASPNETCORE_ENVIRONMENT=Test` → rate limiting disabled, implemented via `appsettings.Test.json` with `"GeneralRules": []`. But .NET configuration binds arrays **by index** — an empty array overrides nothing, so the `{"Endpoint": "*", "Period": "1m", "Limit": 100}` rule from `appsettings.json` survived and 429'd the fully-parallel Playwright run (hundreds of requests/minute from one IP).

**Fix:** `if (!app.Environment.IsEnvironment("Test")) app.UseIpRateLimiting();` in `Program.cs`. Scalar override (`EnableEndpointRateLimiting: false`) alone was insufficient because with endpoint limiting off, the wildcard rule applies globally.

### Fix 3: Form labels not programmatically associated (app bug — a11y + 2 test failures)

**Root cause:** Playwright's `getByLabel` requires real association (`htmlFor`/`id`, wrapping, or ARIA) — visual adjacency of `<label>` and `<input>` does nothing. Every form in the new nurse/accountant/admin pages had unassociated labels, so tests couldn't locate Hospital Name, Amount, Systolic, etc.

**Fix:** `htmlFor`/`id` pairs on all 14 fields across the three pages. Accessibility improvement in its own right (screen readers now announce fields correctly).

### Fix 4: ProtectedRoute rendered `null` for blocked roles (app bug — blank page)

**Root cause:** On role mismatch, the redirect to `/unauthorized` lives in a `useEffect`, but the render path returned `null`. If the client-side navigation is ever cancelled by a racing navigation, the user is stranded on a permanently blank page (observed as `- alert` only in Playwright page snapshots under parallel CPU contention).

**Fix:** Blocked path now renders the same spinner treatment with "Redirecting to unauthorized page…" — fails closed with visible feedback instead of blank.

### Fix 5: Master password hardcoded to the wrong stack (test bug — masked by Fix 6's absence)

**Root cause:** `portals-flow.spec.ts` used `MasterPass123!` (dev stack) against the test stack, where compose sets `Master@123`. Login silently failed and the test then asserted against the login page.

**Fix:** `process.env.TEST_MASTER_PASSWORD || 'MasterPass123!'` at both call sites (`.env.test` already defines it).

### Fix 6: `loginAs` swallowed login failures (test-design bug)

**Root cause:** The robust `loginAs` waited for URL change, caught the timeout, and continued — so wrong credentials produced misleading "element not found" failures downstream, and worse, the cross-portal smoke test could **pass falsely** (its assertions were satisfiable by the login page itself).

**Fix:** `loginAs` now throws `Login as <email> failed: still on /login after submit` — failures are explicit and name the credentials/stack as the thing to check.

### Fix 7: Strict-mode violation + non-existent textarea + fixed sleeps (test bugs)

- "Record Vitals" matched both the sidebar tab and the form's submit button → tab locator scoped to `getByRole('list')`, submit disambiguated with `.last()`.
- Admin settings test filled `page.locator('textarea').first()` — the page renders `<input>`s; now uses the `descriptionInput` label locator.
- Wrong-role test's fixed 2s sleep flaked under parallel workers (proven: passes in isolation, fails in parallel) → `expect(...).toPass({ timeout: 15_000 })` polling for URL **or** body evidence of the block.

---

## Test count delta

| Suite | Before (00:44 run) | After |
|---|---|---|
| Playwright total (ui + api + e2e) | 4 failed / 73 passed | **0 failed / 77 passed (14.1s)** |
| .NET (in Docker build gate) | 158 passed / 1 skipped | 158 passed / 1 skipped (no regressions) |

---

## Lessons learned (worth remembering)

1. **.NET JSON config arrays merge by index — they never replace.** `"GeneralRules": []` in an environment-specific appsettings does NOT clear the base array. To remove array-configured behavior per environment, gate it in code.

2. **Playwright `getByLabel` requires programmatic association.** A `<label>` sitting next to an `<input>` is invisible to it. When a `getByLabel` locator fails on a page that "obviously has that label," check for `htmlFor`/`id` first — and fix the app, not the locator, because it's an accessibility bug too.

3. **Login helpers must fail loudly.** A `loginAs` that swallows failure converts credential/config bugs into cryptic downstream locator errors — and can let smoke tests pass against the login page itself.

4. **Fixed sleeps flake under `fullyParallel`.** Any `waitForTimeout` gate on hydration/redirect will eventually fail under CPU contention. Poll conditions (`toPass`, `waitForURL`, web-first assertions) or don't assert.

5. **Seeds added after the fact must not live inside `if (tenant == null)` blocks.** Those only run on empty databases; late-added seed data needs its own idempotent, existence-checked block.

6. **Debug failing E2E in isolation before "fixing" the test.** The wrong-role failure looked like an app bug, passed solo, failed in parallel — the isolation run (plain Chromium script, 10s of URL/body polling) proved the app correct and the test's fixed sleep guilty in minutes.

---

## Out of scope (intentional, not addressed)

- Committing any of the changes (per project convention — awaiting user commit, both repos)
- Rebuilding the **dev** stack (`make up --build`) — dev containers still run pre-fix images; the dev DB will pick up nurse/accountant seed on its next API restart
- Seeding nurse/accountant for **new** hospitals created via tenant provisioning (only city-general is seeded)
- The untracked `AddHospitalServicesTable` migration and remaining portal polish from the prior agent
- Contact-sales parallel-mode flake — passed in isolation and in the final full run; not root-caused further
- pgvector semantic RAG, MinIO storage backend, storage-service .NET 8 → 10 upgrade (all previously deferred)

---

## Ready for user commit

**mediconyx:**

```text
M  mediconyx-api/Program.cs
M  mediconyx-ui/app/portal/admin/page.tsx
M  mediconyx-ui/app/portal/accountant/page.tsx
M  mediconyx-ui/app/portal/nurse/page.tsx
M  mediconyx-ui/components/ProtectedRoute.tsx
M  readme.md
(plus prior agent's: 5 portal pages, AddHospitalServicesTable migration)
```

Suggested commit message:

```
fix: seed nurse/accountant, disable rate limit in Test, a11y labels

- Seed Nurse + Accountant users for city-general idempotently so
  existing databases receive them (outside the tenant-null block).
- Skip UseIpRateLimiting in the Test environment — appsettings.Test.json
  could not clear GeneralRules because JSON config arrays merge by
  index; the 100 req/min wildcard rule kept 429ing the e2e suite.
- Associate form labels with inputs (htmlFor/id) on nurse vitals,
  accountant payment, and admin settings forms — fixes Playwright
  getByLabel lookups and screen-reader announcement.
- ProtectedRoute: render a redirecting state instead of null for
  blocked roles so a cancelled navigation never strands a blank page.
- readme: document the two new seeded accounts.
```

**mediconyx-e2e:**

```text
A  tests/api/portals.spec.ts            (prior agent)
A  tests/e2e/portals-flow.spec.ts       (prior agent + this session's fixes)
A  tests/helpers/portal-api-client.ts   (prior agent + fixes)
A  tests/pages/portals.page.ts          (prior agent + fixes)
```

Suggested commit message:

```
test: portal API + E2E suites, 77/77 green against test stack

- Level 2: 21 portal API tests (patients, appointments, departments,
  services, invoices, tenant settings, multi-tenant isolation, write
  flows) plus stack-health probe that works without swagger.
- Level 3: full portal E2E flows for doctor, patient, admin, nurse,
  accountant, receptionist + cross-portal role-access matrix.
- loginAs throws on failed login; master password env-driven via
  TEST_MASTER_PASSWORD; strict-mode-safe nav locators; condition
  polling instead of fixed sleeps; recordPayment posts to /payments.
```

---

**Plan complete. Awaiting user commit per project convention.**
