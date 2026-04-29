# Contacting Sales & Sales Dashboard — Tasks

[x] TASK-001: Implement sales data model in EF Core
- Description: Create entities and migrations for SalesRequest, SalesRequestAttachment, SalesRequestNote, AuditLog, OnboardingTimelineEvent; extend User with Sales role.
- Dependencies: None
- Effort: 1d
- Priority: High
  - [x] Define enums and entity classes
  - [x] Configure relationships, indexes, constraints
  - [x] Create initial migration and update database

[x] TASK-002: Build SalesRequestsController (public + authenticated)
- Description: Implement endpoints for public submission, list/filter, detail, create internal, claim/unassign, status transitions, attachments upload, notes.
- Dependencies: TASK-001
- Effort: 2d
- Priority: High
  - [x] POST /api/sales/requests/public (captcha + rate limit)
  - [x] GET /api/sales/requests with filters/pagination
  - [x] GET /api/sales/requests/{id}
  - [x] POST /api/sales/requests (internal create)
  - [x] POST /api/sales/requests/{id}/claim | /unassign
  - [x] PATCH /api/sales/requests/{id}/status
  - [x] POST /api/sales/requests/{id}/attachments (multipart)
  - [x] POST /api/sales/requests/{id}/notes

[x] TASK-003: Build SalesMetricsController
- Description: Implement KPIs and trend aggregations endpoints.
- Dependencies: TASK-001
- Effort: 1d
- Priority: High
  - [x] GET /api/sales/metrics/kpis
  - [x] GET /api/sales/metrics/trends?bucket=week|month|year
  - [ ] Optimize queries and add indexes/materialized views if needed

[x] TASK-004: Build AdminSalesUsersController
- Description: Create/list/activate/deactivate Sales users.
- Dependencies: TASK-001, TASK-005
- Effort: 0.5d
- Priority: High
  - [x] POST /api/admin/sales-users
  - [x] GET /api/admin/sales-users
  - [x] PATCH /api/admin/sales-users/{id}

[x] TASK-005: Extend auth for Sales role + RBAC
- Description: Add Sales role to claims, protect endpoints with role-based attributes, update login.
- Dependencies: None
- Effort: 0.5d
- Priority: High
  - [x] Seed roles and ensure role claim issuance
  - [x] Add [Authorize(Roles="Sales")] and [Authorize(Roles="Master")] where required
  - [x] Verify token generation/refresh for Sales

[x] TASK-006: Contact Sales UI page
- Description: Implement `app/contact-sales/page.tsx` with validation, captcha, file upload, success flow.
- Dependencies: TASK-002
- Effort: 0.75d
- Priority: High
  - [x] Build form with field validation
  - [x] Integrate captcha and submit to public endpoint
  - [x] Handle attachments with size/type validations
  - [x] Show confirmation and error states

[x] TASK-007: Sales Dashboard KPIs
- Description: Implement KPI tiles bound to `/api/sales/metrics/kpis`.
- Dependencies: TASK-003
- Effort: 1d
- Priority: High
  - [x] Create `components/sales/KpiTiles.tsx`
  - [x] Fetch metrics and render total onboarded, in-progress, active, pending, deactivated
  - [x] Add loading/error states

[x] TASK-008: Trend bar chart
- Description: Implement trend bar chart for requests increase/decrease.
- Dependencies: TASK-003
- Effort: 0.5d
- Priority: Medium
  - [x] Create `components/sales/TrendBarChart.tsx`
  - [x] Render grouped data per time bucket, delta indicators
  - [x] Period toggle (W/M/Y)

[x] TASK-009: Pipeline table & filters
- Description: Implement table with filters (status, region, owner, search, pagination).
- Dependencies: TASK-002
- Effort: 1d
- Priority: High
  - [x] Create `components/sales/PipelineTable.tsx`
  - [x] Wire filters to API
  - [x] Add pagination and empty states

[x] TASK-010: Request detail view
- Description: Implement `components/sales/RequestDetail.tsx` with timeline, notes, attachments, status transitions.
- Dependencies: TASK-002
- Effort: 1d
- Priority: High
  - [x] Detail fetch and display
  - [x] Add note creation
  - [x] Upload/list attachments
  - [x] Status transition with reason codes

[x] TASK-018: Pricing & payment fields in model and endpoints
- Description: Extend SalesRequest with pricing/payment fields and implement PATCH endpoints.
- Dependencies: TASK-001, TASK-002
- Effort: 1d
- Priority: High
  - [x] Add fields: subscriptionPlan, billingCycle, currency, quotedPrice, seats, paymentStatus
  - [x] PATCH /api/sales/requests/{id}/pricing
  - [x] PATCH /api/sales/requests/{id}/payment-status
  - [x] Audit changes and validate inputs

[x] TASK-019: Revenue metrics endpoints
- Description: Implement `/api/sales/metrics/revenue` aggregations (quoted vs realized, ARPU).
- Dependencies: TASK-001, TASK-003, TASK-018
- Effort: 0.75d
- Priority: High
  - [x] Group by time bucket (month/year)
  - [x] Compute quotedRevenue, realizedRevenue, arpu
  - [x] Indexing and optional caching

[x] TASK-020: UI pricing widgets
- Description: Add pricing section in Request Detail and display payment status.
- Dependencies: TASK-018
- Effort: 0.75d
- Priority: Medium
  - [x] Inputs for plan, billing cycle, currency, quoted price, seats
  - [x] PaymentStatus control (role-gated)
  - [x] Validation and feedback states

[x] TASK-021: Revenue charts & KPIs
- Description: Add revenue cards and trend chart to Sales Dashboard.
- Dependencies: TASK-019
- Effort: 1d
- Priority: Medium
  - [x] KPI tiles: quoted revenue, realized revenue, ARPU
  - [x] Trend chart bound to `/metrics/revenue`
  - [x] Period toggle (M/Y)

[ ] TASK-011: Master dashboard sales user panel
- Description: Add sales user management panel to `app/portal/master/page.tsx`.
- Dependencies: TASK-004
- Effort: 0.75d
- Priority: Medium
  - [ ] List users with status and last login
  - [ ] Create and activate/deactivate users
  - [ ] CSV export

[x] TASK-012: Notifications
- Description: Send email notifications for new requests and key status changes.
- Dependencies: TASK-002
- Effort: 0.75d
- Priority: Medium
  - [x] SMTP client configuration
  - [x] Notification templates
  - [x] Trigger on create/status change

[x] TASK-013: Rate limiting, captcha, file security
- Description: Add rate limits to public endpoint, server-side captcha verification, file type/size checks, antivirus hook.
- Dependencies: TASK-002
- Effort: 0.75d
- Priority: High
  - [x] Apply rate limiting policies
  - [x] Verify captcha tokens server-side
  - [x] Validate uploads and call antivirus hook

[ ] TASK-014: Analytics performance & caching
- Description: Improve metrics/trends via indexes/materialized views and optional Redis caching.
- Dependencies: TASK-003
- Effort: 1d
- Priority: Medium
  - [ ] Add indexes and measure P95
  - [ ] Implement optional Redis cache
  - [ ] Materialized views (if necessary)

[ ] TASK-015: Testing
- Description: Implement API unit/integration, UI unit/E2E, a11y tests.
- Dependencies: All feature endpoints/UI
- Effort: 2d
- Priority: High
  - [ ] API unit tests (services/validators)
  - [ ] API integration tests with PostgreSQL (Testcontainers)
  - [ ] UI unit tests (KPI, chart, table, pricing widgets)
  - [ ] E2E: contact form, sales login, status transitions
  - [ ] E2E: pricing update and payment status update
  - [ ] Accessibility checks (axe)

[ ] TASK-016: CI/CD & Docker Compose
- Description: Update pipelines, add Compose services, run migrations/seed roles.
- Dependencies: TASK-001, TASK-005
- Effort: 1d
- Priority: Medium
  - [ ] API pipeline (build/test/containerize/migrate)
  - [ ] UI pipeline (build/containerize)
  - [x] Compose for api/ui/postgres/redis/smtp (dev)
  - [x] Env vars for pricing/revenue features and Redis config

[ ] TASK-017: Observability & error handling
- Description: Structured logging, correlation id, ProblemDetails responses.
- Dependencies: TASK-002
- Effort: 0.5d
- Priority: Medium
  - [ ] Add global exception handling and ProblemDetails
  - [ ] Add request correlation id propagation
  - [ ] Ensure logs are structured JSON

[x] TASK-018: Update docker-compose.yaml with Redis and service wiring
- Description: Add Redis and ensure api/ui/postgres/smtp services run together with proper env, networks, healthchecks.
- Dependencies: TASK-001, TASK-003, TASK-014
- Effort: 0.5d
- Priority: High
  - [x] Add `redis` service (image, ports, volume, healthcheck)
  - [x] Ensure `api` reads Redis connection from env (e.g., REDIS_CONNECTION)
  - [x] Define `postgres` service with persistent volume and env vars
  - [x] Wire `api` service (depends_on: redis, postgres; ports; env; restart policy)
  - [x] Wire `ui` service (depends_on: api; ports; env)
  - [x] Add dev `smtp` (MailHog) service for notifications
  - [x] Define networks for internal communication and expose only required ports
