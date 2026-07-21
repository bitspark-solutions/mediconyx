# 2026-07-21 — Multi-Branch Support (Max Healthcare model)

## Goal

One hospital brand (Tenant) operating **multiple branches** — like Max Healthcare in India (Max Saket, Max Patparganj, …) running one unified HIS: patient records shared across all branches, operations (beds, admissions, billing, appointments) scoped per branch. Confirmed by user as a target requirement; design should match how Max standardizes operations across units, improved where we can.

## Core model decision: what is shared vs branch-scoped

| Tenant-shared (brand level, one record across branches) | Branch-scoped (per location) |
|---|---|
| Patient (visit any branch, one record) | Ward, Bed |
| User/staff (belongs to tenant; has a *primary* branch) | Admission (via Bed; denormalize `BranchId` for query simplicity) |
| DoctorProfile (can practice at multiple branches) | Appointment (happens at a branch) |
| Department, HospitalService (catalog) | DoctorSchedule (availability per branch) |
| Tenant settings, modules, subscription | Invoice / BedCharge (billing per branch) |

This matches Max: unified EMR (shared patient) + per-unit operations.

## New entity

```
Branch: Id, TenantId, Name, Code (unique per tenant), Address, City, Phone, Email,
        IsMain (exactly one per tenant), IsActive, audit fields
```

## Migration & backfill strategy (no downtime, no data loss)

1. Create `Branches` table.
2. For every existing tenant, insert "Main Branch" (`IsMain = true`, address copied from `Tenant.Address`).
3. Add nullable `BranchId` to `Wards`, `Beds`, `Appointments`, `Admissions`, `Invoices`, `Users` (as `PrimaryBranchId`).
4. Backfill all existing rows → tenant's Main Branch. Make `BranchId` required where appropriate (Ward/Bed).
5. Indexes: `(TenantId, BranchId)` on each scoped table.

## Auth / visibility rules (keep simple, Max-style)

- JWT gains an optional `branch_id` claim = user's `PrimaryBranchId` (no breaking change; old tokens just lack it until re-login).
- **Tenant Admin**: sees and manages all branches (branch switcher in UI).
- **Branch staff** (Doctor/Nurse/Receptionist/Accountant): default view = own branch; tenant-level records (patients, doctor directory) stay visible.
- API pattern: endpoints accept optional `?branchId=`; when omitted, non-admin staff default to their branch, admins get all. Manual `.Where()` filtering extended with branch — same convention as TenantId today.

## Staged execution (each stage independently shippable + tested)

- **Stage 1 — Foundation**: Branch entity + `BranchConfiguration` + migration with Main-Branch backfill + `BranchController` (CRUD, `ward.manage`-style permission via `TENANT_SETTINGS_EDIT`) + branch management UI page + unit tests.
- **Stage 2 — IPD**: `BranchId` on Ward/Bed (+DTOs, controller validation, bed-board branch filter, cabin board included).
- **Stage 3 — Flows**: `BranchId` on Appointment, Admission, Invoice + `PrimaryBranchId` on User (employee UI field, JWT claim).
- **Stage 4 — Polish**: branch switcher component in header, role-aware default scoping, E2E coverage (multi-branch isolation tests), seed demo tenant with a 2nd branch.

## Risks / watch-outs

- **Manual filtering**: every scoped query must add branch filtering — same risk class as TenantId today; mitigate with E2E cross-branch isolation tests.
- **JWT-baked branch claim**: staff changing branches need re-login (same as roles).
- **Admissions span branches on transfer**: transfer across branches = AdmissionTransfer with beds in different branches; billing uses per-day bed rate already, so it just works — but discharge/bill views must not assume single branch.
- **Uniqueness**: ward `Code` is unique per tenant today → relax to unique per (TenantId, BranchId) in Stage 2.

## Open questions (defaults chosen, can revisit)
- Departments/services shared vs per-branch → **shared catalog** (Max standardizes; hospitals rarely want per-branch department lists).
- Cross-branch patient billing consolidated vs per-branch → **per-branch invoices** (matches how units bill), brand-level revenue report can aggregate later.
