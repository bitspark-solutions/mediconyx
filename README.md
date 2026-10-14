# Mediconyx

**Multi-tenant Hospital Information System with built-in AI** — the Max Healthcare model: one platform, many hospital brands (tenants), each with multiple physical branches, roles, and billing.

Built with **.NET 10** (ASP.NET Core, EF Core), **Next.js 16 / React 19**, PostgreSQL, and a purpose-built storage service. Mediconyx covers the operational spine of a hospital: sales pipeline → tenant provisioning → OPD/IPD → clinical records → double-entry accounting → AI clinical decision support.

> Status: active development. The modules marked *roadmap* below have tenant toggles but no domain logic yet — they are current work scope, not shipped features.

## What's real

| Domain | Status |
|---|---|
| Multi-tenant / multi-branch isolation (tenants, branches, `branch_id` JWT claims) | ✅ working |
| Sales pipeline & tenant provisioning (lead → qualify → approve → provision → live, auto `MAIN` branch, module binding) | ✅ working |
| RBAC with 8 role portals (Master, Admin, Doctor, Nurse, Receptionist, Accountant, Patient, Sales) + fine-grained permissions | ✅ working |
| OPD: appointments, token flow, queue | ✅ working |
| IPD: wards, beds, admissions, discharges, transfers | ✅ working |
| Clinical records: SOAP notes, prescriptions, vitals, attachments | ✅ working |
| Billing & invoicing + **double-entry accounting** (append-only ledger, chart of accounts, fiscal periods, BFRS-aligned, departmental P&L, hospital KPIs) | ✅ working |
| AI assistant (RAG over patient context, role-scoped prompts, citations) | ✅ working (prompt-retrieval; semantic embeddings on roadmap) |
| Secure storage service (checksummed uploads, HMAC-signed URLs, soft-delete purge) | ✅ working |
| Pharmacy, Lab, Radiology, Emergency, Inventory, HR/Payroll | 🚧 roadmap |

## Quick start

Prerequisites: Docker, .NET 10 SDK, Node 22+.

```bash
cp mediconyx-api/appsettings.json.example mediconyx-api/appsettings.json   # fill dev values
docker compose up -d          # or: make up
```

| Service | URL |
|---|---|
| Web app | http://localhost:9673 |
| API | http://localhost:9765 |
| Storage service | http://localhost:8890 |
| PostgreSQL (host) | localhost:9543 |

Run tests:

```bash
dotnet test mediconyx-api/Tests          # 176 unit/integration tests
```

## Demo accounts (local development seeds only)

The dev seeder creates a sandbox hospital ("City General") so you can log in immediately. These accounts exist **only** in local databases created from these seeds — remove or reseed them before any real deployment.

| Role | Email | Password |
|---|---|---|
| Platform Master | `master@mediconyx.local` | `MasterPass123!` |
| Hospital Admin | `admin@citygeneral.local` | `Hospital@123` |
| Doctor | `dr.khan@citygeneral.local` | `Doctor@123` |
| Nurse | `nurse@citygeneral.local` | `Nurse@123` |
| Receptionist | `reception@citygeneral.local` | `Reception@123` |
| Accountant | `accountant@citygeneral.local` | `Accountant@123` |
| Patient | `karim.hassan@patient.local` | `Patient@123` |

## Architecture

```
Next.js 16 UI (:3000 → :9673)
      │ REST + JWT (access/refresh, branch_id claim)
      ▼
ASP.NET Core API (:8080 → :9765)
 ├─ Multi-tenancy: TenantId/branch scoping, seeded + self-healing branches
 ├─ RBAC: PermissionCatalog constants + [HasPermission] handler
 ├─ Accounting: immutable journal entries → materialized balances → P&L / Trial Balance
 ├─ AI: RagQueryService (context assembly, role-scoped system prompts, citations)
 └─ Storage client ──► storage-service (:8890, X-Api-Key, HMAC URL tokens)
      ▼
PostgreSQL (:5432 → :9543)
```

- `mediconyx-api/` — the API (EF Core, xUnit suite under `Tests/`)
- `mediconyx-ui/` — Next.js app; per-role portals under `/portal/*`
- `storage-service/` — standalone .NET minimal API for file custody (date-partitioned store, DICOM-aware, soft-delete purge)
- `PROJECT_OVERVIEW.md` — the full domain walkthrough (onboarding → clinical → billing)

## Security notes

- JWT access/refresh rotation; per-request tenant checks.
- File access requires short-lived HMAC-signed URLs; server-to-server calls need `X-Api-Key`.
- `appsettings.json` is developer-local; the template is committed as `appsettings.json.example`.
- Known gaps being actively closed: global EF query filters for tenancy, clinical access-audit trail, authorization for roadmap modules.

## License

© 2026 Bitspark Solutions — all rights reserved (showcase repo).
