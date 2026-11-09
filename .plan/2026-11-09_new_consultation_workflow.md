# Plan — Make NewConsultationWorkflow fully functional + E2E tested

Date: 2026-11-09 · Status: in progress

## Problem
`mediconyx-ui/components/clinical/NewConsultationWorkflow.tsx` (shared by doctor + receptionist portals):
- Only "register patient" persists (`POST /api/patient`). The Consultation Details (department / service type / priority) are never saved — no appointment is created.
- Patient search does nothing; "Recent Lookups", "Today's Queue", and the 4 metric cards are hardcoded demo data.
- Related rot: `components/receptionist/AppointmentBooking.tsx` assigns a **random doctor id** (`Math.floor(Math.random()*100)`) when picking from search results; E2E helper `createAppointment` has a wrong payload (missing `departmentId`, sends `durationMinutes`) and is never called.

## API contracts verified (no backend changes needed)
- `POST /api/appointment` → `{patientId, doctorId, departmentId, appointmentDate, startTime:"HH:mm", endTime:"HH:mm", type, reason, notes}`; `doctorId` = **User id** (not DoctorProfile id); branch defaults to tenant main. Returns 201 + `AppointmentDto`. Requires `APPOINTMENT_CREATE`.
- `GET /api/doctor` → `{doctors: DoctorProfileDto[]}` — use `userId` for the appointment's `doctorId`.
- `GET /api/department` → `{departments: DepartmentDto[]}`.
- `GET /api/appointment/slots?doctorId=<userId>&date=` → derived from `DoctorSchedules` (`DoctorId` is User id there too). **Demo seeders create no schedules → empty slots**; UI must offer manual time fallback.
- `GET /api/patient` has **no server-side search param** → filter client-side (same as doctor portal does).
- `AppointmentType` enum: Consultation, FollowUp, Checkup, Emergency, Procedure, Telemedicine (JSON accepts string or numeric).
- Seed note: doctor user for new appointments — new doctor creation via `POST /api/Doctor` returns profile with `userId`.

## Changes
1. **UI** — rewrite `NewConsultationWorkflow.tsx`:
   - Real patient search (`GET /api/patient?pageSize=100`, client-side filter) + select-existing flow.
   - Register-new still posts `POST /api/patient`, then chains into the appointment.
   - Department dropdown ← real `GET /api/department`; Doctor dropdown ← real `GET /api/doctor` (shows accepting patients); appointment `doctorId = profile.userId`.
   - Date (default today) + time: quick-pick slots from `/api/appointment/slots` when doctor schedules exist, **manual start/end time fallback** (default 30 min) when none.
   - Service Type → `AppointmentType` (Initial Consultation→Consultation, Follow-up Visit→FollowUp, Lab Review→Checkup, Diagnostic Scan→Procedure). Priority: emergency→Type Emergency, urgent→`[URGENT] ` reason prefix.
   - `aiNote` textarea → passed as appointment `reason`/`notes` (real data, not decorative).
   - Metric cards + Today's Queue ← real `GET /api/appointment?dateFrom=<today>&dateTo=<today>`; Recent Lookups ← real recent patients.
   - Success message includes the created appointment id; portal appointment lists will show it.
2. **E2E** (`../mediconyx-e2e`):
   - Fix `createAppointment` helper payload in `tests/helpers/portal-api-client.ts`.
   - New `tests/api/consultation-flow.spec.ts`: self-provision department → doctor (+schedule for today) → patient → appointment via the exact chain the UI uses; assert 201, list visibility, slot exclusion, cancel.
   - New `tests/e2e/new-consultation-flow.spec.ts` (browser, video on): receptionist logs in → New Consultation tab → fill + submit → success; appointment verified via API.
3. **Verification**: `npx tsc --noEmit` (UI); unit tests unchanged (no API edits); bring up test stack (`docker compose -f docker-compose.test.yml up -d`); run `npm run test:env`; collect videos if green.
