# Session Log: Onboarding UI Story Verification & Demo Video

**Date**: 2026-08-29  
**Goal**: Run, confirm, refile, and verify the full browser-driven onboarding flow in `mediconyx-e2e` against the running test stack.

---

## 1. Root Causes & Fixes Applied

### A. SHA-256 Collision on Fixtures
- **Problem**: `SalesRequestsController.UploadAttachment` computes SHA-256 of uploaded files to deduplicate uploads. The 3 test fixture files (`license.pdf`, `tax-certificate.pdf`, `business-registration.pdf`) had byte-for-byte identical contents. Consequently, `tax-certificate.pdf` and `business-registration.pdf` were discarded as duplicate attachments and returned the existing `license.pdf` record.
- **Fix**: Added unique content/comments inside each fixture file in `tests/fixtures/` so each generates a distinct SHA-256 hash.

### B. Upload Loop Determinism
- **Problem**: Rapid successive calls to `setInputFiles` collided with `disabled={uploadingFile}` state and async re-fetching.
- **Fix**: In `tests/e2e/onboarding-story-ui.spec.ts`, explicitly waited for each uploaded file card to be visible in the DOM (`toBeVisible({ timeout: 15_000 })`) before uploading the next file.

### C. Session Clearing on First Admin Login
- **Problem**: Step 6 logs in as the newly provisioned hospital admin using the temporary password retrieved from MailHog. Because `page.goto('/login')` was called with the existing `Sales` user session still active in `localStorage` (`auth-storage`), `AuthRedirectGuard` immediately bounced navigation back to `/portal/sales`, preventing the login form from rendering.
- **Fix**: In `uiLogin()`, added explicit cookie and `localStorage`/`sessionStorage` clearing followed by navigation to `${BASE_URL}/login`.

---

## 2. Test Execution & Verification

1. **Full UI Flow (`npm run test:onboarding:ui`)**:
   - **Result**: Passed (37.3s).
   - **Steps verified**:
     1. Contact Sales public landing page submission (`Dr. UI Demo`, hospital metadata, optional notes).
     2. Sales rep login (`sales@mediconyx.local`), search request, Claim, Qualify.
     3. Hospital Profile form submission (100 beds, address, contact details), Technical requirements (departments list, IT contact), Modules selection (`standard`), 3 mandatory document uploads (`license.pdf`, `tax-certificate.pdf`, `business-registration.pdf`), and Pricing quote application (Standard tier, 25 seats, BDT 25,000).
     4. Sales Approve, Confirm Payment, and Provision Tenant.
     5. MailHog welcome email arrival with temporary admin password.
     6. New hospital admin first login and landing on `/portal/admin`.

2. **Full E2E Suite (`npm run test:env`)**:
   - **Result**: **153 passed / 0 failed / 4 skipped** (59.0s).

3. **Slow-Mo Demo Video Generation (`npm run test:onboarding:ui:slow`)**:
   - Generated MP4 recording at `test-videos/2026-08-29_00-41-28_onboarding-story-ui-slow/onboarding-story-ui-Full-o-d7700-ision-→-email-→-first-login-e2e.mp4`.
