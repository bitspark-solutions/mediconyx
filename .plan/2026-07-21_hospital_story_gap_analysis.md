# 2026-07-21 — Hospital Story Gap Analysis & Completion Plan

## Story verification results

| # | Story requirement | Status | Evidence |
|---|---|---|---|
| 1 | Master admin has power over the whole platform | ✅ Done | Master gets `permission: all` claim (`PermissionAuthorizationHandler`, `PermissionService.cs:104`); master portal exists (`/portal/master`) |
| 2 | Hospital applies → sales evaluates/qualifies → approval → onboarding | ✅ Done | Pipeline `Pending → Qualified → InProgress → AwaitingPayment → Provisioning → Active` (`SalesRequestStatus.cs`); qualify/reject/suspend/reactivate/claim/transfer/notes endpoints (`SalesRequestsController.cs`) |
| 3 | Provisioning creates tenant + admin + credentials | ✅ Done | `TenantProvisioningService.cs` (transactional tenant + admin user + module flags + subdomain); welcome email with temp password (`ProvisioningController.cs:91-99`) |
| 4 | Hospital uploads logo, name | ✅ Done | `POST/GET /api/tenant-settings/logo`, `PUT /api/tenant-settings` (`TenantSettingsController.cs`); Hospital Settings tab in admin portal |
| 5 | Hospital puts in locations | ⚠️ Partial | Single free-text `Tenant.Address` only. No multiple branches/locations support |
| 6 | Employee onboarding | ⚠️ Partial | API exists (`POST /api/UserManagement`, `POST /api/doctor`) but **no UI anywhere** — no page/component creates a user or doctor |
| 7 | Custom roles/designations per hospital | ⚠️ Partial | `User.Designation` (free-form, 150 chars) + `UserRoleAssignments` table exist in DB (migration `20260720000100_AddUserDesignationAndMultiRole`); displayed in public doctor directory. **But**: `CreateUserRequest`/`UpdateUserRequest` DTOs don't expose `Designation`; no endpoint grants/revokes additional roles; roles themselves are a fixed 8-value enum (`UserRole.cs`) — no tenant-defined custom roles |
| 8 | Doctor who is also staff (director + practices) | ⚠️ Partial | Backend permission union works at login (`PermissionService.GetPermissionsForUserAsync` merges primary + additional roles into JWT). **But**: `CreateDoctor` rejects existing emails (`DoctorController.cs:109-112`) — cannot attach a `DoctorProfile` to an existing staff user; no API to manage `UserRoleAssignment`; no UI |
| 9 | IPD admissions | ✅ Done | Full CRUD: wards, beds, admissions, transfers, discharge summaries, bed board (`IPDController.cs` — 18 endpoints); bed-board UI with ward creation |
| 10 | ICU / PICU | ✅ Done | `WardTypes`: General, ICU, PICU, NICU, DayCare, Maternity, Pediatric, Geriatric, Surgery (`Ward.cs:51-66`) |
| 11 | Day care (children & elderly) | ✅ Done | `WardTypes.DayCare` + `AdmissionType.DayCare` (`Admission.cs:53`); Pediatric/Geriatric ward types |
| 12 | Per-bed-day billing for admissions | ✅ Done | `BedBillingService` idempotent per (AdmissionId, ChargeDate); `generate-bill`/`bill`/`billing/charges` endpoints; currency hardcoded BDT |

## Gaps to close (priority order)

### P1 — Employee onboarding UI + designation/multi-role exposure
The story's core missing piece. DB and permission engine are ready; the surface is not.

1. **API — DTOs**: add `Designation`, `PrimaryDepartmentId`, `AdditionalRoles: UserRole[]` to `CreateUserRequest`, `UpdateUserRequest`, `UserDto`, `UserDetailDto`.
2. **API — UserService**: persist `Designation`/`PrimaryDepartmentId`; sync `UserRoleAssignments` (add/remove rows) on create/update.
3. **API — DoctorProfile for existing staff**: new endpoint `POST /api/doctor/attach-profile` (body: `userId` + doctor fields) that creates a `DoctorProfile` for an existing user and adds the `Doctor` role to their `UserRoleAssignments` (instead of `CreateDoctor`'s reject-if-email-exists path). This is the "director who also practices" enabler.
4. **UI — hospital admin employee management**: new Employees section (list, search, create, edit, deactivate) with fields: name, email, phone, role (enum dropdown), designation (free text), department, additional roles (multi-select). Wire to `/api/UserManagement` + `/api/doctor/attach-profile`.
   - Note: current `/portal/admin` page is actually platform/master-flavored (Tenants, review queue, "Master View"). Hospital admin needs its own employees area — decide placement (likely a new `/portal/admin/employees` page or a tab), keeping master view intact.

### P2 — Multi-location support
5. `TenantLocation` entity (name, address, city, phone, isMain) or structured JSONB on Tenant; expose in tenant settings API + UI. Defer if single-address is acceptable for now — confirm with user.

### P3 — True custom roles (optional, bigger)
6. Tenant-defined roles would require replacing the `UserRole` enum usage with a `Role` table per tenant + per-tenant `RolePermission` rows. Large refactor touching JWT claims, `HasPermission`, seeder, and all `Roles=` attributes. **Recommend**: free-text `Designation` + the fixed enum covers the stated story ("roles and designations custom" ≈ designation + permission set). Only build tenant-custom roles if a hospital genuinely needs a new *permission bundle*, not just a new title.

## Suggested execution order
1. DTO + UserService changes (API) — small, testable
2. `POST /api/doctor/attach-profile` + tests
3. Employees UI (list/create/edit) incl. designation + additional roles
4. E2E: extend `mediconyx-e2e` — API tests for new endpoints; portal test for employee onboarding incl. director-who-practices scenario
5. (If confirmed) multi-location entity + UI

## Watch-outs
- Permissions are baked into JWT at login — after granting an additional role, the user must re-login (or we must force token refresh) to get new claims.
- Any new tenant-scoped query must include the manual `.Where(x => x.TenantId == tenantId)` filter (no global query filters in this codebase).
- Role enum changes ripple into `PermissionSeeder`, JWT role claim, frontend role→route maps (`login/page.tsx`, `AuthRedirectGuard.tsx`) and `ProtectedRoute` — avoid touching the enum for P1/P2.
