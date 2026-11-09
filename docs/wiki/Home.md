# Mediconyx Wiki

Multi-tenant hospital management SaaS. Generated 2026-11-01 from the **graphify** code graph
(5,231 nodes · 9,827 edges · 316 communities · built from commit `6b0a259f`).

Interactive graph: http://localhost:28848/visualizer?project=mediconyx ·
Tree view: http://localhost:28848/tree?project=mediconyx ·
Full report: `graphify-local-server/data/mediconyx/graphify-out/GRAPH_REPORT.md`

## Pages

- [[Architecture]] — services, ports, request flow, deployment topology
- [[Backend-API]] — controllers, services, middleware, DI, hard conventions
- [[Domain-Model]] — entities by subsystem (tenancy, RBAC, clinical, IPD, billing, accounting)
- [[Frontend]] — Next.js portals, apiClient, Zustand stores, route guards
- [[Multi-Tenancy-and-Branches]] — tenant isolation convention, branch scoping stages
- [[Storage-Service]] — uploads, signed URLs, API-key auth, purge job
- [[Testing]] — unit tests, E2E strategy, video collection
- [[Graph-Index]] — subsystem → graphify community map (for graphify_query entry points)

## Repos

| Path | What |
|---|---|
| `mediconyx-api/` | .NET 10 ASP.NET Core + EF Core + PostgreSQL |
| `mediconyx-ui/` | Next.js 16 + React 19 + Tailwind 4 + Zustand |
| `storage-service/` | .NET 8 minimal API — file storage + signed URLs |
| `../mediconyx-e2e/` | Playwright E2E suite |

## Stacks

| Stack | UI | API | DB | Storage | MailHog |
|---|---|---|---|---|---|
| Dev | :9673 | :9765 (+/swagger) | :9543 | :8890 | :8125 |
| Test | :9674 | :9766 | :9544 | :8891 | :8126 |

Logins: dev `master@mediconyx.local`/`MasterPass123!`, test `Master@123` — full matrix in `readme.md`.

## Regenerating this graph

```bash
# full rebuild (~4 min, do NOT use the MCP graphify_build tool — it times out)
curl -X POST http://localhost:28848/api/build \
  -H "Content-Type: application/json" \
  -d '{"path":"/app/workspaces/mediconyx","force":true}'

# regenerate GRAPH_REPORT.md + communities (note: needs MSYS_NO_PATHCONV on Git Bash)
MSYS_NO_PATHCONV=1 docker exec graphify-local-server \
  graphify cluster-only /app/data/mediconyx

# incremental update after code changes (no full rebuild)
MSYS_NO_PATHCONV=1 docker exec graphify-local-server \
  graphify update /app/workspaces/mediconyx
```

Graph freshness check: compare `git rev-parse HEAD` against the commit in GRAPH_REPORT.md.

## Core hubs (god nodes — where everything flows through)

1. `mediconyx_api.Models` (227 edges) · `mediconyx_api.Models.DTOs` (200)
2. `ApplicationDbContext` (136) · `mediconyx_api.Data` (82)
3. `SalesRequest` (71) · `User` (55) · `Tenant` (51)
4. `SalesRequestsControllerTests` (43) · `AppointmentDto` (43)
