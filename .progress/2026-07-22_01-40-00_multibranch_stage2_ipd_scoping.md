# 2026-07-22 — Multi-Branch Stage 2 (IPD Scoping) Complete

Wards, beds, and cabins are now scoped to branches. Suite: **152 passed / 0 failed / 3 skipped** (grew +1: per-branch uniqueness test now checks both sides).

## Backend
- `Ward.BranchId` + `Bed.BranchId` (denormalized from ward for cheap filtering); configs in `Data/Configurations/` updated (`(TenantId, BranchId)` indexes, FKs with Restrict).
- Ward code uniqueness moved from per-tenant to **per (TenantId, BranchId)** — same code allowed across branches.
- Migration `20260721000300_AddBranchIdToWardsBeds`: nullable → backfill to tenant's MAIN branch → NOT NULL; index swap.
- `?branchId=` filters on wards/beds/cabins/bed-board; ward creation defaults to main branch + validates branch; bed creation validates branch/ward consistency; all DTOs carry BranchId/BranchName.

## UI
- Bed-board: branch filter dropdown (when >1 branch), branch badges on ward cards, branch picker in create-ward modal.
- New `/portal/admin/branches` page: create/edit/promote/deactivate branches, main badges — linked from the admin tab bar.

## Real bug found & fixed (fresh-DB gap)
The Stage 1 backfill (`INSERT ... SELECT FROM Tenants`) runs inside `MigrateAsync()` — **before** seeding on a fresh DB, so freshly seeded/provisioned tenants never got a main branch (surfaced when test-db's tmpfs was wiped).
- `Data/Seeding/BranchSeeder.cs` — idempotent healer run at every startup (creates MAIN, or promotes first branch).
- `TenantProvisioningService` now creates MAIN inside the provisioning transaction.
- Verified: Platform healed automatically on restart; City General now has MAIN + S2UT (real second branch with ICU ward).

## E2E repairs
- tier2 duplicate-code test now verifies both sides of per-branch uniqueness (reject same-branch, allow cross-branch).
- `ensureWard` race-proofed: always creates its own ward (parallel tests deleting shared wards caused flaky 400s); code ≤20 chars.

## Verification
- `dotnet build` clean; 170/171 unit tests; **152/152 E2E green**; dev + test stacks rebuilt and healed (MAIN for both tenants on dev).
- Manual test: `/portal/admin/branches` (manage branches), bed-board branch dropdown on `http://localhost:9673`.

## Remaining: Stage 3 (BranchId on Appointment/Admission/Invoice, staff PrimaryBranchId + JWT claim) and Stage 4 (header switcher, role-aware scoping, cross-branch E2E).
