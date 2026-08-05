# 2026-08-05 — Multi-branch Stage 3: Flows (Appointment/Admission/Invoice + User.PrimaryBranchId + JWT claim)

## What was done

Stage 3 of `.plan/2026-07-21_multibranch_support.md` — branch scoping for operational flows.

**Models** (one class per file preserved):
- `Appointment`, `Admission`, `Invoice`: new required `BranchId` + `Branch` nav.
- `User`: new nullable `PrimaryBranchId` + `PrimaryBranch` nav (null = tenant-wide staff).

**EF configs**: `(TenantId, BranchId)` index + FK→Branches (Restrict) on the three flows; `UserConfiguration` FK→Branches SetNull.

**Behavior**:
- `AppointmentService` / `InvoiceService`: `?branchId=` list filter; create resolves branch from request (validated: must belong to tenant + active) else tenant's main branch (`ArgumentException` → 400). DTOs expose `BranchId`/`BranchName`.
- `IPDController` admissions: `?branchId=` filter; `CreateAdmission` sets `BranchId = bed.BranchId`; `TransferAdmission` re-points `BranchId` to the destination bed's branch (cross-branch transfer just works); `AdmissionDto` carries `BranchId`/`BranchName`.
- `UserService`: `PrimaryBranchId` on create/update (tenant+active validated); `UserDetailDto` exposes id+name.
- `JwtService`: `branch_id` claim when `PrimaryBranchId` set (old tokens lack it until re-login — same as roles). `ClaimsHelper.GetBranchId` added.

**Migration**: `20260805000100_AddBranchIdToFlowsAndUsers` (hand-written, attributes in place) — nullable add → backfill (admissions inherit their bed's branch; appointments/invoices → tenant Main Branch) → NOT NULL → indexes/FKs. Snapshot synced via throwaway-scaffold trick; `has-pending-model-changes` clean.

**UI**: employees page — Branch column + "Primary branch" select in create/edit modal (fetches `/api/branch`).

## Verification

- API build: 0 warnings/errors.
- Unit tests: **176 passed / 1 skipped** (added 6: branch default/invalid/filter for appointments + invoices; seeded a Main Branch in the two service fixtures).
- UI: `npx tsc --noEmit` clean.
- Test stack rebuilt (`test-api`, `test-ui`); migration applied + backfill verified in `mediconyx_test`.
- E2E: **151 passed / 0 failed / 4 skipped** — the single onboarding-story failure was stale MailHog host-port binding after a Docker Desktop restart (`docker restart mediconyx-test-mailhog` fixed; test re-run green).

## Gotchas discovered

- Docker Desktop restart can leave container port bindings dead while the container looks "Up" — `docker restart <container>` rebinds. Symptom: `ECONNREFUSED ::1:8026` from host while MailHog answers fine inside the network.
- Windows TCP exclusion range 1025–1124 covers the test SMTP host port 1026 (didn't block the run, but watch it).

## State / next

- Dev stack rebuilt (`api`, `ui`); migration applied to dev DB — all 8 existing admissions backfilled to their bed's branch (no appointments/invoices in dev).
- Stage 3 complete. Next: **Stage 4 — Polish** (header branch switcher, role-aware default scoping using the `branch_id` claim, cross-branch isolation E2E, seed demo 2nd branch).
