# Domain Model

Entity map by subsystem, from the graphify community analysis. Every tenant-owned
entity has `TenantId`; flow entities (Stage 3+) also carry `BranchId`.

## Tenancy & onboarding

| Entity | Notes |
|---|---|
| `Tenant` | hospital tenant — address, business reg no, contact (community 17) |
| `HospitalProfile` | logo, bed capacity, address lines, docs (33) |
| `SalesRequest` | public intake → qualification → provisioning, god node, 71 edges (9) |
| `SalesRequestStatus`, `OnboardingTimelineEvent` | pipeline state + audit trail (89, 25) |
| `TechnicalRequirements` | concurrent users, dept count, data-migration flag (74) |
| `ModuleSelection` | per-tenant module toggles, enforced by middleware (67) |
| `TenantSettings`/`TenantSettingsDto` | editable config incl. subscription (41) |
| `Branch`/`BranchDto` | multi-branch (Max Healthcare model); `IsMain` backfill (5) |

## Identity & RBAC

| Entity | Notes |
|---|---|
| `User` | god node, 55 edges; `PrimaryBranchId` → JWT `branch_id` (13) |
| `UserRole`, `UserRoleAssignment` | multi-role incl. custom designations (43) |
| `Permission`, `RolePermission` | catalog of 34 seeded permissions (50) |
| `Invitation` | email invite → `AcceptInvitation` flow (73) |
| `DoctorProfile` | doctor-as-staff duality via `attach-profile`; fee, duration, bio (48) |

## Clinical

| Entity | Notes |
|---|---|
| `Patient` | gender enum, blood group; self-registration service exists (26, 56) |
| `Appointment` | `BranchId`, check-in, cancel reason (39) |
| `VitalSign` | BP sys/dia, glucose, per appointment (62) |
| `MedicalNote`, `Prescription` | kebab-case routes — explicit `[Route]` required |
| `TelehealthSession` | session per appointment (75) |
| `FileAttachment`/`FileAttachmentDto` | category, content type, signed download (52) |

## IPD (inpatient)

| Entity | Notes |
|---|---|
| `Ward` | `BranchId`, per-branch ward-code uniqueness (76) |
| `Bed` | `BranchId`, `CabinType`, facilities (86) |
| `Admission` | ICU/PICU/daycare types, attending doctor (38) |
| `AdmissionTransfer` | bed-to-bed moves |
| `BedBoardBedDto` | bed-board projection w/ occupancy (55) |
| `AdmissionBillDto` | per-bed-day billing output (58) — `BedBillingService` (community 80) |

## Billing & accounting

| Entity | Notes |
|---|---|
| `Invoice`, `InvoiceLineItem` | `BranchId`, discount, balance due (40, 61) |
| `Account`, `AccountType` | chart of accounts, cost centers (27) |
| `JournalEntry` | `BranchId`, postings, `JournalEntrySourceType` (66) |
| `FiscalPeriod`, `AccountBalance` | period close (49) |
| `LedgerService` | balance sheet + income statement reports (42, 63, 68) |
| `PaymentStatus` | Pending/Paid/Failed/Refunded enum (community 2) |

## Provisioning flow (community 2)

`ITenantProvisioningService.ProvisionTenantAsync` — sales request → tenant +
hospital profile + MAIN branch + admin user + default chart of accounts.
