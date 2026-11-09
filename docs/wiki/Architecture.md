# Architecture

Three deployable services + PostgreSQL + MailHog, orchestrated by `docker-compose.yml`
(dev) and `docker-compose.test.yml` (test). Both stacks run side-by-side on separate
ports and Docker networks (`mediconyx` / `mediconyx-test`).

```
Browser ──► UI (Next.js 16, :9673/:9674)
              │  NEXT_PUBLIC_API_BASE_URL (build-time inlined!)
              ▼
          API (ASP.NET Core 10, :9765/:9766, /swagger dev-only)
              ├──► PostgreSQL (:9543/:9544)  EF Core migrations at boot
              ├──► storage-service (.NET 8, :8890/:8891)  X-Api-Key
              └──► MailHog (web :8125/:8126)  Smtp__Host=mailhog:1025
```

## Key architectural facts

- **`Program.cs` is 14 lines** — everything lives in `Extensions/ServiceCollectionExtensions.cs`
  and `Extensions/WebApplicationExtensions.cs` after the 2026-08 refactor.
- **Manual migrations at boot** — `DatabaseInitializer.MigrateWithRetryAsync` retries
  10 × 3s because Docker daemon restarts ignore compose healthchecks.
- **`NEXT_PUBLIC_*` is build-time inlined** — UI env changes require an image rebuild,
  not a container restart.
- **No EF global query filters** — tenant/branch isolation is explicit
  `.Where(x => x.TenantId == tenantId)` on every query. See [[Multi-Tenancy-and-Branches]].
- **JSON conventions** — `JsonStringEnumConverter` (string or numeric enums accepted)
  and `UtcDateTimeJsonConverter` (date-only strings treated as UTC, required for
  Npgsql timestamptz) registered globally.
- **Rate limiting** via AspNetCoreRateLimit; JWT bearer auth; DataProtection keys
  are in-container only (ephemeral).

## Middleware pipeline (graph community 24)

`CorrelationIdMiddleware` → `GlobalExceptionHandlerMiddleware` → module-toggle
gate → auth. Kebab-case routes need explicit `[Route("api/kebab-name")]` —
`[controller]` does NOT map dashes.

## AI / RAG

`GenericEmbeddingService` + `GenericChatCompletionService` (graph communities 69, 83)
talk to an external LLM for embeddings and chat. **Endpoints 500 without an LLM
`ApiKey` in appsettings** — dev-safe to leave unset.

## Windows port gotcha

Hyper-V reserves shifting TCP ranges (seen: 7871–7970, 7937–8036, 1035–1134).
That's why storage is on 8890/8891 and dev MailHog moved to 8125/2525.
Check `netsh int ipv4 show excludedportrange protocol=tcp` before picking host ports.
