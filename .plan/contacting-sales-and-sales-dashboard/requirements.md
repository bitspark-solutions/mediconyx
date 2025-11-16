# Contacting Sales & Sales Dashboard — Requirements

## Feature Overview
- Provide a “Contact Sales” entry point for hospitals to submit sales requests and onboarding intent.
- Enable Master Admins to create and manage Sales Team accounts (username/password) from the Master Dashboard.
- Implement Sales Team login and a separate Sales Dashboard focused on pipelines, onboarding statuses, and performance metrics.
- Build secure REST APIs for managing sales requests, onboarding workflows, users, and analytics.

## Functional Requirements
- Contact Sales
  - Public form (hospital name, contact person, email, phone, region, notes) with CAPTCHA, rate limiting, and email confirmation/notification to Sales.
  - Persist request with initial status `pending` and audit metadata (ip, user agent, createdBy, timestamps).
  - Optional attachment support (PDF/doc) with antivirus scan and size/type validation.
  - Pricing inputs: desired subscription plan, billing cycle, estimated seats, budget range, currency.
- Master Admin: Sales User Management
  - Create Sales users (username, email, password, role=Sales).
  - Activate/deactivate accounts; enforce strong passwords and password reset flow.
  - View list of sales users with statuses and last login; export CSV.
- Sales Authentication
  - Email/username + password login; JWT access + refresh tokens with RBAC (`Master`, `Sales`, future roles).
  - Session persistence, token rotation, device-aware login history, and logout.
- Sales Dashboard
  - KPI tiles: total onboarded customers, in-progress onboarding, active, pending, deactivated.
  - Bar chart: weekly/monthly trend of sales requests (increase/decrease YoY/MoM/WoW).
  - Pipeline views and filters by status, region, owner; search and pagination.
  - Request detail page: timeline, notes, attachments, status transitions with reason codes.
  - Actions: claim/unassign request, change status (`pending` → `in_progress` → `active` or `deactivated`), schedule follow-ups.
  - Export reports (CSV) and shareable dashboards (role-gated).
  - Pricing & payments: set subscription plan, billing cycle, currency, quoted price; track `paymentStatus` (pending, paid, failed, refunded).
- Analytics & Reporting
  - Aggregated metrics endpoints for counts by status, conversion rates, average time-to-onboard, and trends.
  - Drill-downs by region, sales owner, and time buckets; caching for heavy aggregations.
  - Financial metrics: MRR/ARR, total quoted revenue, realized revenue (paid), ARPU, pipeline value by stage, conversion revenue by period, revenue trend chart.
- Notifications & Integrations
  - Email notifications on new requests and key status changes.
  - Webhook/event hooks for future CRM integration; background jobs for email and enrichment.

## Non-Functional Requirements
- Security
  - OWASP Top 10 mitigations: input validation (server-side & client-side), output encoding, HTTPS only, CSRF for forms, SSRF-safe uploads, rate limiting.
  - Password hashing with Argon2id or bcrypt, salted; JWT with short-lived access and long-lived refresh; IP/device metadata logging.
  - RBAC enforcement at route/controller level; audit logs for all status changes and privileged actions.
- Performance
  - P95 latency ≤ 300ms for standard CRUD; ≤ 800ms for analytics endpoints; pagination for lists.
  - Indexed queries; efficient aggregations via SQL GROUP BY, materialized views for heavy dashboards; optional Redis caching.
- Reliability & Ops
  - Centralized structured logging (JSON); error monitoring; health checks; graceful degradation for analytics.
  - Rate limits on public endpoints; backpressure on file uploads; retry policies for background jobs.
- Accessibility & UX
  - WCAG 2.1 AA for forms and dashboards; keyboard navigation; high-contrast charts; responsive design.
- Scalability
  - Horizontal scalability for API; partitioning strategies for large tables; bulk operations for exports.

## User Stories & Acceptance Criteria
- Story: Hospital submits a sales request
  - As a hospital rep, I can submit a contact sales form to request onboarding.
  - Acceptance: Form validates required fields, captcha passes, request saved with status `pending`, confirmation email sent, Sales notified.
  - Acceptance (pricing): If provided, preferred plan/billing cycle/currency/seats/budget are captured.
- Story: Master Admin creates Sales user
  - As a master admin, I can register a sales user with username/password and set activation.
  - Acceptance: User appears in list, can log in if activated, password policy enforced, email invite optional.
- Story: Sales login & logout
  - As a sales user, I can log in and access the Sales Dashboard; I can logout.
  - Acceptance: Valid credentials issue JWTs; invalid credentials rejected; protected routes accessible; logout clears tokens.
- Story: View Sales Dashboard KPIs
  - As a sales user, I see KPIs: total onboarded, in-progress, active, pending, deactivated.
  - Acceptance: KPIs match backend metrics endpoint; numbers update with filters.
- Story: View trend bar chart
  - As a sales user, I see a bar chart indicating requests trend with increase/decrease indicators.
  - Acceptance: Chart displays grouped counts per time bucket; supports period toggles (W/M/Y); performance meets P95 targets.
- Story: Update request status
  - As a sales user, I can move a request through statuses with reason codes.
  - Acceptance: Transitions validated; audit logged; notifications triggered; analytics reflect changes.
- Story: Record pricing and payment status
  - As a sales user, I can set plan, billing cycle, quoted price, currency, and update `paymentStatus`.
  - Acceptance: Fields are validated; changes are audited; financial metrics update; only authorized roles can update payment status.
- Story: Export report
  - As a sales manager, I can export CSV of requests and KPIs.
  - Acceptance: Export includes pricing fields and payment status; respects filters and role permissions; file generated within 5s.

## Tech Stack Decisions (verified)
- Frontend
  - Next.js 16.0.1 (App Router)
  - React 19.2.0
  - TypeScript ^5
  - Zustand ^5.0.8 for client state
  - Tailwind CSS 4
- Backend API
  - ASP.NET Core `net10.0` Web API
  - Entity Framework Core 10 with Npgsql provider
  - JWT via `Microsoft.AspNetCore.Authentication.JwtBearer`
  - Password hashing via `BCrypt.Net-Next`
  - Swagger via `Swashbuckle.AspNetCore`
- Database
  - PostgreSQL (configured via `appsettings.json`)
- Auth & Security
  - JWT access/refresh tokens, RBAC; BCrypt password hashing; CORS via `FrontendUrl`
- Ops
  - Dockerfiles for API and UI; build/test pipelines; structured JSON logging

## Compliance
- GDPR: lawful basis for processing contact data; consent capture for communication; DSAR processes; data minimization; retention policy.
- HIPAA: avoid PHI in sales requests; if PHI present, enforce HIPAA controls (encryption at rest/in transit, access controls, audit logs, BAAs).
- KYC/KYB: optional verification workflow for hospital onboarding; store verification outcomes and evidence securely.

## Integration Points
- Public contact form → POST `/api/sales/requests` (rate-limited, captcha verified).
- Master dashboard → POST `/api/admin/sales-users` for user creation; PATCH `/api/admin/sales-users/:id` for activation.
- Sales auth → POST `/api/auth/login`, POST `/api/auth/refresh`, POST `/api/auth/logout`.
- Requests management → GET/POST/PATCH `/api/sales/requests` with filters and transitions.
 - Metrics → GET `/api/sales/metrics/kpis`, GET `/api/sales/metrics/trends?bucket=week|month|year`, GET `/api/sales/metrics/revenue?bucket=month|year`.
- Notifications → Background jobs queue sending emails/webhooks.
 - Pricing → PATCH `/api/sales/requests/{id}/pricing` (plan, billingCycle, currency, quotedPrice, seats)
 - Payment → PATCH `/api/sales/requests/{id}/payment-status` (pending|paid|failed|refunded)

## Constraints & Assumptions
- Existing Next.js codebase available; adopt the same UI framework and conventions.
- No external CRM yet; webhook interface reserved.
- Public endpoints must be robust against spam; CAPTCHA and IP-based throttling required.
- Chart library must be lightweight and accessible; Chart.js chosen for simplicity and broad support.
- DB migrations and seed data handled via Prisma; analytics may use materialized views refreshed periodically.
 - No card data processing in-app; future integration with payment providers (Stripe, etc.) will avoid PCI scope by tokenizing payments.

## References
- Next.js + Prisma example with Next 15.1.4 and Prisma 6.x: https://www.prisma.io/blog/series/fullstack-nextjs-and-graphql-md1tczpfz1
- NestJS stable 11.1.9 (2025‑11‑14): https://en.wikipedia.org/wiki/NestJS
- PostgreSQL releases (18.1): https://www.postgresql.org/
- Chart.js v4.5.1: https://github.com/chartjs/Chart.js/releases
- Zustand releases v5.x: https://github.com/pmndrs/zustand/releases
