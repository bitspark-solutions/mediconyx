# 2026-07-26 — Tenant Admin Gaps Closed: Module Enforcement + Subscription Management

User asked to fix the "Hospital setup & administration" gap list. Suite: **152 passed / 0 failed / 3 skipped**; 170/171 unit tests. All flows verified live on the test stack.

## 1. Module toggles — editable AND enforced (was: stored, never checked)
- `PUT /api/TenantSettings` now accepts the 12 module flags (null = unchanged); response returns the full DTO incl. modules + `subscriptionExpiresAt`.
- New enforcement chain: `Models/ModuleKeys.cs`, `Services/Common/TenantModuleService.cs` (+interface), and `Middleware/ModuleEnforcementMiddleware.cs` gating route prefixes — `/api/ipd`→IPD, `/api/appointment`→Appointment Scheduling, `/api/telemedicine`→Telemedicine, `/api/invoice`→Billing. Hospital users get a clear 403 ("The X module is not enabled for this hospital…"); **Master bypasses**. Registered after UseAuthorization.
- Settings UI: module cards are now clickable toggles saved via the existing Save button, with a note that disabling blocks features immediately.

## 2. Subscription management (was: display-only)
- New `PUT /api/TenantSettings/subscription/{tenantId}` (**Master-only**, `[Authorize(Roles="Master")]`) — plan, user licenses, start/expiry dates. This is how the owner renews/extends/reseats a hospital.
- **Seat enforcement**: `UserService.CreateUserAsync` counts active users vs `UserLicenses` → 400 "User license limit (N) reached… contact the platform administrator".
- **Expiry enforcement**: login blocked with 401 "Hospital subscription has expired. Contact Mediconyx to renew." (Platform tenant exempt — no expiry set.)
- Settings UI shows the expiry date, red when past.

## 3. Seed fix surfaced by enforcement
The demo hospital seed only enabled OPD/Appointment/Billing/Pharmacy — with enforcement live, IPD and Telemedicine would have been blocked for it. `DemoHospitalSeeder` now also enables IPD + Telemedicine; both live DBs patched.

## Verification (all live)
- Admin disables IPD → `/api/ipd/wards` 403 with message → master 200 → re-enable 200
- Master sets seats=12 (current count) → create user 400 seat-limit → restore
- Master sets expiry in past → login 401 expired → restore → login 200
- Full E2E: 152/152 green; `tsc` clean; build clean.

## Not done (by design)
- Master UI for editing other tenants' subscriptions (API-only for now — owner dashboard item).
- Structured address on the tenant profile (branch addresses cover locations; low value).
