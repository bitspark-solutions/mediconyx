# 2026-07-21 22:46 — Employee management gaps closed (P1 of hospital story gap analysis)

Closed the three P1 gaps from `.plan/2026-07-21_hospital_story_gap_analysis.md`:
designation/multi-role exposure on user management, doctor-profile attach for existing staff,
and a hospital-admin Employees UI.

## Files changed

### API (mediconyx-api)
- `Models/DTOs/CreateUserRequest.cs` — added `Designation`, `PrimaryDepartmentId`, `AdditionalRoles: UserRole[]?`
- `Models/DTOs/UpdateUserRequest.cs` — same three (all optional/null = unchanged)
- `Models/DTOs/UserDto.cs` — added `Designation`, `PrimaryDepartmentId`, `AdditionalRoles: List<string>`
- `Models/DTOs/UserDetailDto.cs` — added `Designation`, `PrimaryDepartmentId`, `PrimaryDepartmentName`, `AdditionalRoles: List<string>`
- `Models/DTOs/AttachDoctorProfileRequest.cs` — NEW (userId + license/specialization/departmentId/duration/fee)
- `Services/Users/UserService.cs` — persist designation/department on create/update (with same-tenant
  department validation); new private `SyncAdditionalRoles` grants/revokes `UserRoleAssignment` rows
  (admin-only on update; primary role excluded); all read paths now Include `PrimaryDepartment` +
  `AdditionalRoles`; update returns a reloaded DTO
- `Controllers/DoctorController.cs` — NEW `POST /api/doctor/attach-profile`
  (`[HasPermission(PermissionCatalog.DOCTOR_PROFILE_EDIT)]`): tenant-scoped user lookup, rejects
  existing profile, validates specialization against `MedicalSpecializations.All`, validates
  department tenancy, creates `DoctorProfile`, adds `UserRole.Doctor` assignment when primary
  role isn't Doctor
- `Tests/UnitTests/UserServiceTests.cs` — +6 tests (designation/dept persistence, cross-tenant dept
  rejection, role sync grant/revoke, non-admin rejection, designation clear)
- `Tests/UnitTests/DoctorControllerTests.cs` — NEW, +6 attach-profile tests (happy path + role grant,
  no-op when already Doctor, duplicate profile 400, cross-tenant 404, bad specialization 400,
  cross-tenant department 400)

### UI (mediconyx-ui)
- `app/portal/admin/employees/page.tsx` — NEW. ProtectedRoute `allowedRoles=["Admin"]`. Employee list
  (name/email/role+additional/designation/department/status) from `GET /api/UserManagement?pageSize=100`;
  create modal (name/email/password/phone/role/designation/department from `GET /api/department`/
  additional-roles chips); edit modal (same minus password+email); deactivate/reactivate via
  `PATCH /api/UserManagement/{id}/toggle-active`; "Make practicing doctor" modal calling
  `POST /api/doctor/attach-profile`. Re-login hint text shown in list header and form.
  Roles sent as numeric enum values (API has no JsonStringEnumConverter).
- `app/portal/admin/page.tsx` — added "Employees" link in the tab bar next to Bed Board

## Verification
- `dotnet build mediconyx-api/mediconyx-api.csproj` — clean, 0 warnings/errors
- `dotnet test mediconyx-api/Tests/mediconyx-api.Tests.csproj` — **170 passed, 1 skipped**
  (baseline 158/1 → +12 new tests, all green)
- `cd mediconyx-ui && npx tsc --noEmit` — clean

## Notes / deferred
- Permissions are baked into JWT at login: granted roles apply only after the employee re-logs in
  (hint text in UI). No token-revocation mechanism exists yet.
- `PrimaryDepartmentId` on update can be set/changed but not cleared (nullable = "leave unchanged"),
  matching existing UpdateDoctor style.
- "Make practicing doctor" is offered on all active employees; duplicate-profile attempts get a
  clear 400 from the API.
- Not done (from gap analysis): P2 multi-location, P3 tenant-defined custom roles, E2E tests
  for the new endpoints/UI (suggested next: extend `mediconyx-e2e`).
