# 2026-07-26 — Full Onboarding Story: Live Browser + Pipeline Walkthrough

Complete tenant story executed end-to-end on the test stack, browser-visible for act 1.

## Act 1 — New tenant requests contact (headed Chromium, watched live)
- `tests/e2e/contact-sales-flow.spec.ts --headed`: 4/4 passed — form fill, validation, landing→contact navigation, second submission. Request #4 created ("Pacocha Casper General Hospital", contact Mr. Victor Blick).

## Act 2 — Sales takes over
- Claim (204, owner=Sales User) → qualify (status Qualified, timeline event recorded)
- Hospital profile (legal name, license, tax ID, address, primary contact) → 201
- Module selection (OPD, IPD, Lab, Billing, Appointments, Telemedicine) → 201
- Pricing (Standard, 600,000 BDT/yr, 25 seats, 12-month contract) → 204
- Payment status → Paid → status moved to AwaitingPayment

## Act 3 — Master provisions
- First attempt correctly rejected ("must be AwaitingPayment, current InProgress") — pipeline guard works
- `POST /api/sales/requests/4/provision` as master → tenant **#3**, subdomain `pacocha-casper-general-hospital`, admin user created, modules mapped from selection, **MAIN branch auto-created** (validates the Stage-2 provisioning fix in the real path)

## Act 4 — New tenant's first day
- Welcome email in MailHog: "Welcome to Mediconyx - Your Account is Ready!" with temp password
- **First login: SUCCESS** — Mr. Victor Blick, Admin, tenant 3
- Sees own settings (Standard plan, 25 seats, IPD + telemedicine enabled), sees MAIN branch, sees **0** of City General's patients (isolation holds)

## Pipeline notes for product polish (not bugs, but friction)
- Status flow has a hidden dependency: profile/modules creation moves the request to InProgress, but provisioning requires AwaitingPayment — sales must explicitly set status after payment. A "mark paid" action could auto-advance it.
- WebBridge daemon still needs the browser extension installed for real-session passes (user action pending).
