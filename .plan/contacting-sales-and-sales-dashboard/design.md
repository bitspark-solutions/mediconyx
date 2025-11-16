# Contacting Sales & Sales Dashboard — Design

## Architecture Overview
- UI: Next.js 16 (App Router), React 19, Tailwind CSS 4, Zustand 5 for local state.
- API: ASP.NET Core `net10.0` Web API with EF Core 10 + Npgsql.
- Auth: JWT (access/refresh) with role-based authorization (`Master`, `Sales`).
- Data: PostgreSQL; optional Redis for caching analytics and rate limiting.
- Integrations: SMTP for email notifications; optional webhooks for CRM.

## Components
- UI
  - Public Contact Form: `app/contact-sales/page.tsx` with validation, captcha, and file upload.
  - Master Dashboard: Sales User Management panel in `app/portal/master/page.tsx` (create/activate/deactivate Sales users).
  - Sales Dashboard: `app/portal/sales/page.tsx` with KPI tiles, trend bar chart, pipeline list, request detail.
- UI modules: `components/sales/KpiTiles.tsx`, `TrendBarChart.tsx`, `PipelineTable.tsx`, `RequestDetail.tsx`.
- API Controllers
  - `SalesRequestsController`: CRUD, claim/unassign, status transitions, attachments.
  - `SalesMetricsController`: KPIs, trend aggregations, revenue metrics.
  - `AdminSalesUsersController`: create/list/activate/deactivate Sales users.
  - `NotificationsController` (optional): email/webhook triggers.

## Data Model
- `User` (existing): extend with `Role` enum including `Sales`; fields: `Id`, `Username`, `Email`, `PasswordHash`, `IsActive`, `Role`, `LastLoginAt`, `CreatedAt`, `UpdatedAt`.
- `SalesRequest`
  - `Id` (uuid), `HospitalName`, `ContactPerson`, `Email`, `Phone`, `Region`, `Notes`, `Status` (enum: `pending`, `in_progress`, `active`, `deactivated`),
  - Pricing: `SubscriptionPlan`, `BillingCycle` (enum: `monthly`, `annual`), `Currency` (ISO 4217), `QuotedPrice` (decimal), `Seats` (int),
  - Payment: `PaymentStatus` (enum: `pending`, `paid`, `failed`, `refunded`), `PaidAt` (nullable),
  - `OwnerSalesUserId` (nullable FK to `User`), `SourceIp`, `UserAgent`, `ClaimedAt`, `ActivatedAt`, `DeactivatedAt`, `CreatedAt`, `UpdatedAt`.
- `PaymentRecord` (optional for future integration)
  - `Id`, `SalesRequestId` (FK), `Provider` (string), `ExternalPaymentId` (string), `Amount` (decimal), `Currency`, `Status`, `CreatedAt`, `UpdatedAt`.
- `SalesRequestAttachment`
  - `Id`, `SalesRequestId` (FK), `FileName`, `StoragePath`, `MimeType`, `SizeBytes`, `Sha256`, `UploadedAt`.
- `SalesRequestNote`
  - `Id`, `SalesRequestId` (FK), `AuthorUserId` (FK), `Note`, `CreatedAt`.
- `AuditLog`
  - `Id`, `EntityName`, `EntityId`, `Action`, `ActorUserId`, `OldValues` (jsonb), `NewValues` (jsonb), `Reason`, `CreatedAt`.
- `OnboardingTimelineEvent`
  - `Id`, `SalesRequestId` (FK), `Type`, `Description`, `HappenedAt`.

## API Contracts
- Auth (existing in `AuthController`): reuse for Sales login.
- Admin Sales Users
  - `POST /api/admin/sales-users`
    - Body: `{ username, email, password, isActive }`
    - 201 → `{ id, username, email, isActive, role: "Sales" }`
  - `GET /api/admin/sales-users?status=active|inactive`
    - 200 → `[{ id, username, email, isActive, lastLoginAt }]`
  - `PATCH /api/admin/sales-users/{id}`
    - Body: `{ isActive: boolean }` → 204
- Public Contact Sales
  - `POST /api/sales/requests/public`
    - Body: `{ hospitalName, contactPerson, email, phone, region?, notes?, captchaToken, subscriptionPlan?, billingCycle?, seats?, budgetRange?, currency? }`
    - 201 → `{ id, status: "pending" }`
- Sales Requests (authenticated: role `Sales`)
  - `GET /api/sales/requests?status=&ownerId=&region=&q=&page=&pageSize=` → 200 list + pagination
  - `POST /api/sales/requests` → create internal request
  - `GET /api/sales/requests/{id}` → detail with attachments, notes, timeline
  - `POST /api/sales/requests/{id}/claim` → assign to current sales user
  - `POST /api/sales/requests/{id}/unassign`
  - `PATCH /api/sales/requests/{id}/status` → Body: `{ status, reason }`
  - `POST /api/sales/requests/{id}/attachments` (multipart)
  - `POST /api/sales/requests/{id}/notes` → Body: `{ note }`
- Metrics & Reports (authenticated: role `Sales` or `Master`)
  - `GET /api/sales/metrics/kpis`
    - 200 → `{ totalOnboarded, inProgress, active, pending, deactivated }`
  - `GET /api/sales/metrics/trends?bucket=week|month|year`
    - 200 → `[{ period: "2025-W45", count, deltaPercent }]`
  - `GET /api/sales/metrics/revenue?bucket=month|year`
    - 200 → `[{ period: "2025-11", quotedRevenue, realizedRevenue, arpu }]`
  - `GET /api/sales/reports/export.csv` → filtered CSV

- Pricing & Payment Endpoints (authenticated: role `Sales`)
  - `PATCH /api/sales/requests/{id}/pricing`
    - Body: `{ subscriptionPlan, billingCycle, currency, quotedPrice, seats }` → 204
  - `PATCH /api/sales/requests/{id}/payment-status`
    - Body: `{ paymentStatus }` (pending|paid|failed|refunded) → 204

## Workflows
- Contact Sales Submission
  1. User fills form → captcha verified → POST `/public` → save `pending` + audit.
  2. Email notification to Sales; optional enrichment job.
- Sales Login & Dashboard
  1. Sales user logs in → JWT issued → Sales dashboard loads KPIs (`/kpis`) and trend (`/trends`).
  2. Pipeline table fetches `/requests` with filters.
- Status Transition
  1. Sales updates status via `/status` → validate allowed transitions → write audit + timeline → recalc metrics.
  2. Notifications for key transitions (`active`, `deactivated`).
- Pricing & Payment Update
  1. Sales sets pricing via `/pricing` → validate fields → audit change → update pipeline value.
  2. Sales updates payment status via `/payment-status` → audit → update realized revenue metrics.
- Admin Sales User Management
  1. Master registers Sales user → email invite (optional) → activate/deactivate via PATCH.

## Error Handling Strategy
- Consistent JSON responses using ProblemDetails semantics: `{ title, status, errors? }`.
- Validation errors: 400 with field-wise messages.
- Auth errors: 401; authorization errors: 403.
- Concurrency: use rowversion/timestamps to detect conflicts; respond 409.
- File uploads: validate type/size; antivirus scan hook; if scan fails → 422.
- Global exception filter with structured logging (correlation id).

## Security Measures
- JWT access (short-lived) + refresh (rotated); store refresh in DB with device fingerprint.
- RBAC: `[Authorize(Roles = "Master")]` for admin endpoints; `[Authorize(Roles = "Sales")]` for sales endpoints.
- Rate limiting for public `/public`; captcha verification; IP and UA recorded.
- Input validation server-side; output encoding; CORS restricted to `FrontendUrl`.
- Audit logging for privileged operations and status changes.

## Deployment Plan
- Docker Compose services: `api`, `ui`, `postgres`, `redis` (optional), `smtp` (dev mock).
- API migrations on startup gated by env; seed roles (`Master`, `Sales`).
- CI/CD
  - API: `dotnet build`, `dotnet test`, container build/push; run EF migrations.
  - UI: `npm ci`, `npm run build`, container build/push.
  - Security scans for dependencies; secrets via env vars.

## Testing Strategy
- API
  - Unit: services and validators; password/JWT services.
  - Integration: controller tests with `WebApplicationFactory`, Testcontainers for PostgreSQL; auth/authorization tests.
  - Performance: metrics endpoints P95 ≤ 800ms, pagination correctness.
- UI
  - Unit: components (KPI tiles, chart) with Jest/RTL.
  - E2E: Playwright/Cypress for contact form submission, sales login, status transitions.
  - Accessibility: axe checks for WCAG AA.

## Diagram References
- Component Diagram
  ```mermaid
  flowchart LR
    UI[Next.js UI]
    API[ASP.NET Core API]
    DB[(PostgreSQL)]
    REDIS[(Redis)]
    SMTP[(Email Provider)]
    UI -->|JWT| API
    API --> DB
    API --> REDIS
    API --> SMTP
  ```

- Sequence: Contact Sales
  ```mermaid
  sequenceDiagram
    participant H as Hospital User
    participant UI as Next.js UI
    participant API as ASP.NET Core
    participant DB as PostgreSQL
    H->>UI: Submit Contact Form + Captcha
    UI->>API: POST /api/sales/requests/public
    API->>DB: Insert SalesRequest(status=pending)
    API-->>UI: 201 {id, status}
    API->>SMTP: Send notification to Sales
  ```

- Deployment
  ```mermaid
  flowchart TB
    subgraph Docker Host
      UI
      API
      DB
      REDIS
    end
    UI --> API
    API --> DB
    API --> REDIS
  ```
