# 2026-07-22 — Tier-4 E2E Coverage: Branches, Cabins, Employees (+edge cases)

Goal: full E2E coverage with edge scenarios for everything built in the cabin/employee/branch workstreams. Suite went from 115 → **155 tests, 152 passed / 0 failed / 3 skipped**.

## New specs (mediconyx-e2e, api layer)

- `tests/api/tier4-branches.spec.ts` (10) — backfill invariant, CRUD + soft-delete visibility, duplicate code (case-insensitive), main-first ordering, promote→demote + restore, main delete/deactivate guards, role 403s, cross-tenant 404, Platform-only list for master.
- `tests/api/tier4-cabins.spec.ts` (17) — catalog shape + 401 unauth, default vs custom facilities, invalid type/kind 400s, plain-bed regression in cabin ward, duplicate number, 403 without `bed.manage`, list filters (status/type), type/facility updates + invalid update, bed-board cabin fields, billing edges: cabin-rate billing (25,000 BDT), bed→cabin transfer rate switch (18,000), occupied-cabin admit/delete guards. Tests clean up (discharge) after themselves.
- `tests/api/tier4-employees.spec.ts` (12) — designation/department/multi-role create (string enums — exercises JsonStringEnumConverter), grant+revoke sync with primary untouched, primary-role dedup, cross-tenant department/user guards, user.manage 403, duplicate email, attach-profile arc (profile created, Doctor in additionalRoles, appears in directory, permission union live on fresh login), primary-is-Doctor no dup assignment, duplicate attach 400, invalid specialization 400.
- `tests/helpers/portal-api-client.ts` — branch/cabin/employee methods + raw bed post/patch/delete helpers for edge payloads.

## Bugs the new tests caught (in the tests themselves, both fixed)
- Unique-code generator produced >20-char codes; server caps at 20 → all creations 400ed. Shortened generator.
- Branch spec races: single-main mutation tests now run serial; backfill assertion relaxed (a MAIN-coded branch exists + exactly one main) since promotion tests change which branch is main.

## Notes
- `CreateUser`/`UpdateUser` return 201 — new specs assert `.ok` instead of hard 200.
- Test DB restored to clean state (MAIN re-promoted, probe branches removed) after an earlier manual smoke left Uttara as main.
