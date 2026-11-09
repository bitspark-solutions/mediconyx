# Backend API (`mediconyx-api`)

.NET 10 ASP.NET Core. Layout after the 2026-08 refactor:

```
mediconyx-api/
  Program.cs                      # 14 lines
  Extensions/                     # ServiceCollectionExtensions, WebApplicationExtensions
  Controllers/                    # thin, permission-gated
  Services/                       # Auth, Appointments, Inpatient, Invoices, Accounting,
                                  # Patients, Users, Invitations, AI, Infrastructure, Common
  Models/ + Models/DTOs/          # one class per file (hard rule)
  Data/
    ApplicationDbContext.cs       # ~70 lines
    Configurations/               # per-entity IEntityTypeConfiguration
    Seeding/                      # DatabaseInitializer + seeders
  Middleware/                     # CorrelationId, GlobalExceptionHandler, module gate
  Swagger/                        # FileUploadOperationFilter, FileUploadSchemaBuilder
  Migrations/                     # hand-written — see conventions below
  Tests/                          # UnitTests + IntegrationTests (xunit + FluentAssertions + Moq)
```

## Controller inventory (from graph)

Clinical/ops: `IPDController`, `AppointmentController`, `MedicalNoteController`,
`PrescriptionController`, `VitalSignController`, `DoctorScheduleController`,
`DoctorController`, `PublicDoctorController`, `FileAttachmentController`.

Tenancy/sales: `SalesRequestsController`, `ApprovalController`, `TenantSettingsController`,
`InvitationsController`, `UserManagementController`, `SalesUsersAdminController`,
`AuthController`, `BranchController`.

Billing/accounting: `InvoiceController`, `JournalEntryController` (+ reports:
balance sheet, income statement via `LedgerService`).

## Hard conventions

- **One class per file** — models, DTOs, enums, helpers. No exceptions.
- **Hand-written migrations MUST carry** `[DbContext(typeof(ApplicationDbContext))]`
  and `[Migration("<id>")]` or EF silently ignores them.
- **Snapshot sync**: after model changes, scaffold a throwaway migration, delete its
  files, verify `dotnet ef migrations has-pending-model-changes` is clean.
- **Tenant filter is manual** `.Where(x => x.TenantId == tenantId)`; `?branchId=`
  filters follow the same pattern (create-time branch validation defaults to Main).
- **Permissions** via `[HasPermission]` — old pre-RBAC tokens get 403 everywhere;
  re-login. JWT carries `tenant_id`, `branch_id`, and a `permission` claim array.
- **Uploads**: unified policy PDF/JPEG/PNG/GIF/DICOM ≤50MB, enforced on both API
  and storage side.

## Build & test

```bash
"C:/Program Files/dotnet/dotnet.exe" build mediconyx-api/mediconyx-api.csproj
"C:/Program Files/dotnet/dotnet.exe" test mediconyx-api/Tests/mediconyx-api.Tests.csproj
# expect ~176 passed / 1 skipped
```

## Seeding

`Data/Seeding/` — `DatabaseInitializer` runs migrations + seeders at boot.
`BranchSeeder` heals fresh-DB tenants (creates MAIN branch); provisioning creates
MAIN for new tenants. Test stack seeds 34 permissions / 95 role-permission rows.
