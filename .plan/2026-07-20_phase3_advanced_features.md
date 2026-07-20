# Phase 3: Advanced Features — Implementation Plan

**Date:** 2026-07-20
**Goal:** Close all gaps in the user story — multi-role staff, IPD with ICU/PICU/DayCare, telemedicine, patient self-registration, multi-modal AI, per-role permissions, doctor profiles.

---

## Scope (from user story)

> "I am an admin, and I have power all over the platform. I onboard a hospital. Hospital onboards after sales evaluation. Hospital uploads logo, name, locations. Hospital onboards employees with custom roles. Doctors are staff who can also be directors/do surgery/prescribe/see patients. Hospitals facilitate patient admission — ICU, PICU, day care for children and elderly."

---

## Architecture: 3 tiers, foundation first

### Tier 1 — Foundation (must come first, everything else depends on it)

#### 1.1 Per-role fine-grained permission system (RBAC)
**Why first:** The IPD, telemedicine, and other features all need permission gating beyond simple role checks.

**New entities:**
- `Permission` (id, name, category, description) — flat list: e.g. "admission.create", "admission.discharge", "bed.assign", "telemedicine.start", "user.manage", "doctor.profile.edit", etc.
- `RolePermission` (role, permissionId) — pre-seeded for each existing role

**New auth infrastructure:**
- `[HasPermission("admission.create")]` attribute on controllers
- Updated `ClaimsHelper` to extract user permissions from JWT
- Migration to seed permissions + role-permission matrix

**Files:**
- `mediconyx-api/Models/Permission.cs` (new)
- `mediconyx-api/Models/RolePermission.cs` (new)
- `mediconyx-api/Services/Auth/PermissionService.cs` (new)
- `mediconyx-api/Middleware/HasPermissionAttribute.cs` (new)
- `mediconyx-api/Migrations/20260720_*_AddRbac.cs` (new)

#### 1.2 User Designation + Multi-role support
**Why first:** "Director who does surgery" requires this.

**Model changes:**
- Add `User.Designation` (string, free-form) — e.g. "Director of Cardiology", "Head Nurse", "Senior Surgeon"
- Add `UserRoles` join table: `(UserId, Role, IsPrimary)` — user can have multiple roles
- Update `DoctorProfile` to be linkable even if user's primary role isn't Doctor (allow Director+Doctor combo)

**Files:**
- `mediconyx-api/Models/UserDesignation.cs` (new, optional — could be on User)
- `mediconyx-api/Models/UserRoleAssignment.cs` (new)
- Update `User.cs`, `CreateUserRequest.cs`, `UserService.cs`
- `mediconyx-api/Migrations/20260720_*_MultiRole.cs` (new)

#### 1.3 Department Head linking
**Why first:** Original plan referenced it; was never added.

**Model change:**
- Add `Department.HeadDoctorId` (nullable FK to DoctorProfile)

**File:** `Department.cs`, migration

---

### Tier 2 — IPD (the core "admit patients" story)

#### 2.1 Ward + Bed + Admission models
**New entities:**
- `Ward` (Id, TenantId, Name, Code, Type: General/ICU/PICU/NICU/DayCare/Maternity/Pediatric/Geriatric, Floor, Capacity, IsActive)
- `Bed` (Id, TenantId, WardId, Number, Status: Available/Occupied/Cleaning/Maintenance/Reserved, Notes)
- `Admission` (Id, TenantId, PatientId, BedId, AdmissionType: Emergency/Elective/Transfer, AdmissionDate, ExpectedDischarge, AttendingDoctorId, Reason, Status: Active/Discharged/Transferred/LAMA, DiagnosisOnAdmission)
- `AdmissionTransfer` (Id, AdmissionId, FromBedId, ToBedId, TransferredAt, TransferredByUserId, Reason)
- `DischargeSummary` (Id, AdmissionId, DischargeDate, DischargingDoctorId, ConditionAtDischarge: Improved/Stable/Critical/LAMA/Deceased, DischargeInstructions, FollowUpDate, MedicationsOnDischarge)

**Files:** `mediconyx-api/Models/Ward.cs`, `Bed.cs`, `Admission.cs`, `AdmissionTransfer.cs`, `DischargeSummary.cs`, migration

#### 2.2 IPD Controller + Service
**Endpoints:**
- `GET /api/ward` — list wards (tenant-scoped)
- `POST /api/ward` — create ward (admin)
- `GET /api/bed?wardId=X&status=Y` — list beds (tenant-scoped, filterable)
- `POST /api/bed` — create bed (admin)
- `PATCH /api/bed/{id}/status` — change status (cleaning/maintenance)
- `GET /api/admission?patientId=X&status=Active` — list admissions
- `POST /api/admission` — admit patient (assigns bed, changes bed status to Occupied)
- `POST /api/admission/{id}/transfer` — transfer to another bed
- `POST /api/admission/{id}/discharge` — discharge (frees bed, requires DischargeSummary)
- `GET /api/admission/{id}/discharge` — get discharge summary
- `GET /api/bed-board` — full board: all wards + beds with current patient, status, attending doctor (for IPD dashboard)

**Files:** `mediconyx-api/Controllers/IPDController.cs`, `mediconyx-api/Services/IPD/IPDService.cs`, `IPD/BedBoardService.cs`, DTOs

#### 2.3 Bed Board frontend
**New page:** `/portal/ipd` or under `/portal/admin/bed-board`
- Visual grid: wards as columns/sections, beds as cards
- Bed card shows: bed number, status (color-coded), patient name if occupied, attending doctor
- Click empty bed → admission modal
- Click occupied bed → patient info + transfer/discharge actions
- Add ward management to admin settings

**Files:** `mediconyx-ui/app/portal/admin/bed-board/page.tsx` (new), `mediconyx-ui/components/ipd/*` (new)

#### 2.4 Admission/Discharge workflow
- Receptionist/Nurse can admit: pick patient → pick ward/bed → pick attending doctor → confirm
- Doctor can discharge: pick admission → fill discharge summary → confirm (auto-frees bed)
- Doctor can transfer: pick admission → pick new bed → reason

---

### Tier 3 — Advanced features

#### 3.1 Per-bed-day billing
**Model change:** `InvoiceLineItem` already exists via Invoice (need to check). Add automatic daily bed-charge line items for active admissions.
**Cron-like:** A background task that runs daily to add bed charges. For MVP, a manual "generate bed charges" endpoint that the admin can trigger.
**File:** `mediconyx-api/Services/Billing/BedChargeService.cs`, controller endpoint

#### 3.2 Patient self-registration
**Endpoint:** `POST /api/auth/register-patient` (public, captcha-protected)
**Flow:** Patient submits form → captcha verified → patient + user account created (role=Patient) → email verification sent (MailHog) → on verification, account activated
**Files:** `PatientRegistrationController.cs`, email template, frontend `/register` page

#### 3.3 Telemedicine (basic, no actual WebRTC)
**Model:** `TelehealthSession` (Id, AppointmentId, RoomName, Status, StartedAt, EndedAt, ProviderId, PatientId, Notes)
**Endpoint:** `POST /api/telemedicine/session` — create session for an appointment, returns a join URL (placeholder — for real WebRTC, integrate with Twilio/Daily.co later)
**Frontend:** Video call placeholder (uses getUserMedia + a stub for now; flag clearly that it's MVP)

#### 3.4 Doctor profile pages
**Endpoint:** `GET /api/public/doctor/{id}` — public doctor profile (name, specialty, dept, bio, available slots)
**Frontend:** `/doctors` public page listing doctors, `/doctors/{id}` for individual profile

#### 3.5 Multi-modal AI (deferred to follow-up)
**Note:** Requires vision-capable LLM (GPT-4o, Claude 3.5 Sonnet, etc.). Schema ready in `LLM` config but no image handling in RAG yet. Add a `FileAttachment.Category=XRayImaging` and extend RAG to include image references. Actual vision analysis depends on LLM provider choice. Mark as follow-up.

---

## Out of scope (explicit)

- Real WebRTC video (use stub + flag for production)
- Multi-modal AI inference (needs vision LLM config)
- pgvector semantic search (deferred feature per PROJECT_OVERVIEW)
- Production email SMTP (MailHog only)
- Mobile app
- Microservices split

---

## File inventory (estimated)

**New models (8):** Permission, RolePermission, UserRoleAssignment, Ward, Bed, Admission, AdmissionTransfer, DischargeSummary
**New controllers (3):** IPDController, PatientRegistrationController, TelemedicineController (basic)
**New services (4):** PermissionService, IPDService, BedBoardService, BedChargeService
**New DTOs (15+):** WardDto, BedDto, AdmissionDto, TransferDto, DischargeDto, PermissionDto, etc.
**New migrations (3):** RBAC, MultiRole, IPD
**New frontend pages (4):** bed-board, /register, /doctors, /doctors/[id]
**New frontend components (6+):** BedCard, WardColumn, AdmissionModal, DischargeForm, DoctorCard, etc.
**Updated models (5):** User, Department, HospitalService, UserRole, Invoice
**Updated controllers (5):** UserManagementController, DoctorController, ServiceController, PatientController, AuthController
**Updated frontend (5):** admin portal (IPD tab), doctor portal (admit/discharge), patient portal (register link)

**Total: ~60 new files, ~15 modified**

---

## Execution order (will be tackled in this order)

1. Tier 1.1 — RBAC permission system
2. Tier 1.2 — Multi-role + Designation
3. Tier 1.3 — Department Head
4. Tier 2.1 — Ward/Bed/Admission models
5. Tier 2.2 — IPD Controller + Service
6. Tier 2.3 — Bed Board frontend
7. Tier 2.4 — Admission/Discharge workflow
8. Tier 3.1 — Per-bed-day billing
9. Tier 3.2 — Patient self-registration
10. Tier 3.3 — Telemedicine stub
11. Tier 3.4 — Doctor profile pages
12. Tier 3.5 — Multi-modal AI (deferred)

---

## Acceptance criteria

✅ Admin can create custom designations per user ("Director of Cardiology" etc.)
✅ A single user can hold multiple roles (e.g., Doctor + Admin via UserRoleAssignment)
✅ Admin can create wards (General, ICU, PICU, NICU, DayCare, Maternity, Pediatric, Geriatric)
✅ Admin can create beds per ward with status tracking
✅ Receptionist can admit a patient to a specific bed (status → Occupied)
✅ Doctor can transfer patient to a different bed
✅ Doctor can discharge patient (bed status → Cleaning, then Available)
✅ Discharge summary is captured
✅ Bed board shows real-time status of all wards/beds
✅ Daily bed charges are generated for admitted patients
✅ Patient can self-register via public form
✅ Doctor profile pages are public
✅ Telemedicine session can be created (stub)
✅ All existing 77 e2e tests still pass
✅ New e2e tests for IPD + custom roles added (target: 100+ total tests)
