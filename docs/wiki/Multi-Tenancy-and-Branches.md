# Multi-Tenancy & Branches

## Tenant isolation

**There are no EF global query filters.** Every query must explicitly filter:

```csharp
.Where(x => x.TenantId == tenantId)
```

`tenantId` comes from the JWT `TenantId` claim (`.GetTenantId` helper, graph
community 47). Missing a filter = cross-tenant data leak — treat every new
query as a code-review checkpoint.

## Multi-branch (Max Healthcare model)

Plan: `.plan/2026-07-21_multibranch_support.md`. `BranchId` follows the same
manual-filter convention as `TenantId`.

| Stage | Status | What |
|---|---|---|
| 1 — Foundation | ✅ | `Branch` entity, `/api/branch` CRUD, migration `20260721000200_AddBranches` + Main backfill |
| 2 — IPD scoping | ✅ | `BranchId` on Ward/Bed, per-branch ward-code uniqueness, `?branchId=` filters, `/portal/admin/branches` UI |
| 3 — Flows | ✅ | `BranchId` on Appointment/Admission/Invoice (migration `20260805000100`, admissions backfilled from bed); `PrimaryBranchId` on User → JWT `branch_id` claim; create-time branch validation defaults to Main; employees UI branch field |
| 4 — Polish | ⬜ next | header branch switcher, role-aware default scoping (`branch_id` claim → default filter for non-admin staff), cross-branch isolation E2E, seed demo 2nd branch |

## Healing & provisioning

- `BranchSeeder` — creates MAIN branch for tenants that predate branches
  (fresh-DB safe).
- `ITenantProvisioningService` — creates MAIN during tenant provisioning.

## Stage 4 implementation notes

- The `branch_id` JWT claim is already emitted — Stage 4 is mostly consuming it:
  a header switcher that stores selection (Zustand), sends `?branchId=` on
  list endpoints, and a server-side default for non-admin staff scoped to their
  `PrimaryBranchId`.
- Cross-branch isolation E2E must self-provision two branches and assert
  list/get endpoints don't leak across them — same pattern as tenant tests.
