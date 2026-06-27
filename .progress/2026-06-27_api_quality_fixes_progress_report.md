# API Quality Fixes — Progress Report

**Date:** 2026-06-27
**Phase:** Production-readiness hygiene — Swagger + HTTPS + Kerberos warnings
**Collaborators:** Mohsin Khan Shuvo + Hermes (collaborative — plan reviewed & approved by Mohsin before implementation)

---

## Summary

Three known noisy warnings in the running `mediconyx-api` Docker stack — Swagger 500 on `/swagger/v1/swagger.json`, "Failed to determine the https port for redirect", and `libgssapi_krb5.so.2: cannot open shared object file` — were diagnosed, planned, and eliminated using canonical industry-recommended fixes. **Two of the three fixes required deviations from the original plan** when empirical testing revealed inaccuracies: the literal `IsDevelopment()` gate would not have silenced the HTTPS warning inside this container (which sets `ASPNETCORE_ENVIRONMENT=Development`), the literal `libkrb5-3` package name was wrong for Ubuntu 24.04 (the shared object lives in `libgssapi-krb5-2`), and a controller-side DTO refactor turned out to be required in addition to the Swashbuckle filter. All four verification checks pass and the full .NET test suite remains green at 158 passed / 1 skipped / 0 failed (+7 new tests, 0 regressions).

---

## Files changed

| File | Status | Lines (final) | Purpose |
|---|---|---|---|
| `mediconyx-api/Swagger/FileUploadSchemaBuilder.cs` | **new** | 55 | Static helper for file-upload schema — extracted so the schema logic is unit-testable in isolation, without Swashbuckle `OperationFilterContext` fakes |
| `mediconyx-api/Swagger/FileUploadOperationFilter.cs` | **new** | 39 | `IOperationFilter` adapter that discovers `IFormFile` params and delegates to the helper |
| `mediconyx-api/Tests/UnitTests/Swagger/FileUploadSchemaBuilderTests.cs` | **new** | 138 | 7 unit tests for the helper (single/multi/empty input, parameter order preservation, hashset projection, dedup) |
| `mediconyx-api/Models/DTOs/UploadFileRequest.cs` | **new** | 19 | DTO for the file-upload form payload — wraps `PatientId`, `AppointmentId`, `Category`, `Description`, `File` |
| `mediconyx-api/Controllers/FileAttachmentController.cs` | modified | 288 (was 292) | `Upload` action refactored from 5 separate `[FromForm]` params to single `[FromForm] UploadFileRequest request` |
| `mediconyx-api/Program.cs` | modified | 548 (was 523) | (a) `AddSwaggerGen(c => c.OperationFilter<FileUploadOperationFilter>())` registration; (b) `UseHttpsRedirection()` gated behind `DOTNET_RUNNING_IN_CONTAINER != "true"` |
| `mediconyx-api/Dockerfile` | modified | 32 (was 16) | Final stage installs `libgssapi-krb5-2` to satisfy Npgsql's GSSAPI probe |

(Plus `.plan/2026-06-27_api_quality_fixes_plan.md` — plan artifact, not part of the PR.)

**Net code delta:** +123 new lines across 4 files, ~50 lines modified across 3 files.

---

## Verification output

All commands run against the live dev stack (`make up`, no rebuild needed — all changes had already been applied during Tasks 1–4).

```text
=== Fix 1: Swagger JSON returns 200 (was 500) ===
Swagger JSON: HTTP 200

=== Fix 2: file-upload endpoints documented with multipart/form-data ===
Endpoints with multipart/form-data: 3
 - POST /api/FileAttachment/upload
 - POST /api/sales/requests/{id}/attachments
 - POST /api/TenantSettings/logo

=== Fix 3: no 'https port' warning in API logs ===
https port warnings in logs: 0

=== Fix 4: no libgssapi warning in API logs ===
libgssapi warnings in logs: 0

=== Regression: full .NET test suite still passes ===
Passed!  - Failed:     0, Passed:   158, Skipped:     1, Total:   159, Duration: 3 s

=== Regression: login still works ===
"accessToken":"eyJhbG...NiIs
OK: login succeeded
```

---

## Per-fix breakdown

### Fix 1: Swagger JSON 500 → 200

**Root cause:** Swashbuckle 10's `SwaggerGenerator.GenerateParametersAsync` rejects any action parameter declared as `[FromForm] IFormFile` at **parameter-discovery time**, before any `IOperationFilter.Apply()` runs. The original Swashbuckle exception message points the developer at the operation-filter pattern, but that pattern cannot rescue a parameter-discovery error — it operates one layer up.

The three file-upload endpoints were not all equally broken:
- `FileAttachmentController.Upload` (line 32) — `[FromForm] IFormFile file` ❌ rejected at parameter discovery
- `SalesRequestsController.UploadAttachment` — bare `IFormFile file` (no `[FromForm]`) ✅ filter handled fine
- `TenantSettingsController.UploadLogo` — bare `IFormFile file` (no `[FromForm]`) ✅ filter handled fine

**Fix (two parts):**
1. Added `FileUploadOperationFilter` + helper + 7 unit tests — canonical Swashbuckle recipe for documenting file uploads. Handles the 2 endpoints that already used bare `IFormFile`.
2. Refactored `FileAttachmentController.Upload` to take a single `[FromForm] UploadFileRequest request` DTO instead of 5 separate `[FromForm]` parameters. Sidesteps parameter-discovery entirely.

**Deviation from plan:** The original plan assumed the filter alone would fix all three upload endpoints. The filter fixed two of them. The third one — `FileAttachmentController.Upload` — needed the controller-side DTO refactor as well, discovered when the subagent empirically ran the smoke test and the Swagger 500 persisted.

**Wire compatibility:** Zero. The multipart form fields are unchanged from the client's perspective — `patientId`, `appointmentId`, `category`, `description`, `file` all bind the same way through the DTO. ASP.NET model binder is case-insensitive for property names by default.

### Fix 2: HTTPS redirect warning → gone

**Root cause:** `UseHttpsRedirection()` runs unconditionally; container speaks HTTP only, so the middleware logs "Failed to determine the https port for redirect" on every request.

**Fix:** Gate the middleware behind `DOTNET_RUNNING_IN_CONTAINER != "true"`. The `DOTNET_RUNNING_IN_CONTAINER` env var is auto-set to `"true"` by Microsoft's .NET base images and unset in normal `dotnet run` / `dotnet publish` workloads — it's the canonical .NET idiom for "am I running inside a container?"

```csharp
var runningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!runningInContainer)
{
    app.UseHttpsRedirection();
}
```

**Deviation from plan:** The plan said `if (IsDevelopment())`. This would NOT have worked because `docker-compose.yml` line 35 sets `ASPNETCORE_ENVIRONMENT=Development` for the api service — meaning `IsDevelopment()` is `true` *inside* the container, so the middleware would still run, so the warning would still fire. The subagent caught this empirically by rebuilding with the literal fix and observing the warning persist, then switched to `DOTNET_RUNNING_IN_CONTAINER` after re-verifying.

**Developer-laptop UX preserved:** `dotnet run` on a developer laptop doesn't set `DOTNET_RUNNING_IN_CONTAINER`, so `runningInContainer` is `false`, so `UseHttpsRedirection()` runs normally there.

### Fix 3: libgssapi warning → gone

**Root cause:** Npgsql eagerly probes `libgssapi_krb5.so.2` at startup even when authenticating with username/password (it's part of the auth provider discovery). The .NET 10 base image (`mcr.microsoft.com/dotnet/aspnet:10.0`) doesn't ship this library by default.

**Fix:** Install it in the Docker final stage via apt-get:

```dockerfile
RUN apt-get update \
 && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*
```

(`--no-install-recommends` keeps the install slim; `rm -rf /var/lib/apt/lists/*` keeps the layer small; both `update` and `install` in one `RUN` to avoid stale-cache reuse.)

**Deviation from plan:** The plan said `libkrb5-3`. Empirically, `dpkg -L libkrb5-3` shows `libkrb5.so.3` and `libkrb5support.so.0` — **but no `libgssapi_krb5.so.2`**. The shared object lives in `libgssapi-krb5-2` on Ubuntu 24.04. Installing the correct package transitively pulls in `libkrb5-3` + `libkrb5support0` anyway. The subagent documented this distinction in an inline Dockerfile comment so future maintainers don't get tripped up the same way.

**Image size delta:** ~+76 MB on disk (libgssapi-krb5-2 + its transitive deps). Acceptable.

---

## Test count delta

| | Before | After |
|---|---|---|
| Passed | 151 | **158** |
| Failed | 0 | 0 |
| Skipped | 1 | 1 |

+7 tests from `FileUploadSchemaBuilderTests.cs`. No regressions. The single skipped test (`TenantProvisioningServiceTests.ProvisionTenantAsync_ShouldRollbackTransaction_WhenErrorOccurs`) was already skipped on `master` and is unchanged.

---

## Lessons learned (worth remembering)

1. **Swashbuckle 10 `IOperationFilter.Apply()` runs AFTER parameter discovery.** Filter-based fixes cannot help with parameter-discovery errors — those need controller-side refactors (DTOs). The original Swashbuckle exception message suggests the filter pattern but that's misleading for `[FromForm] IFormFile` specifically.

2. **`IsDevelopment()` returns true inside the mediconyx-api container** because `docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT=Development`. For "am I in a container?" detection, use `DOTNET_RUNNING_IN_CONTAINER` instead. The .NET base images set this env var to `"true"` automatically.

3. **Ubuntu 24.04 package split:** `libgssapi_krb5.so.2` lives in `libgssapi-krb5-2`. The plan's `libkrb5-3` does NOT contain it. Always verify package contents (`dpkg -L <package>`) when debugging a missing-shared-object error.

4. **Plan deviations are healthy.** All three of this plan's code snippets needed empirical correction. The disciplined process — implementer subagent runs the smoke test, observes the failure, fixes it, re-verifies — caught every issue before user-visible damage. Two-stage review (spec then quality) at the per-task level would have caught these even earlier; for this PR's scope, the empirical verification step was sufficient.

---

## Out of scope (intentional, not addressed)

- Replacing `storage-service` local FS with MinIO/S3
- Upgrading `storage-service` from .NET 8 to .NET 10
- Splitting the 280-line seed block in `Program.cs` into an `IDbSeeder`
- Adding pgvector for semantic RAG retrieval (per PROJECT_OVERVIEW, deferred feature)
- Replacing `ASPNETCORE_ENVIRONMENT=Development` in `docker-compose.yml` with a dedicated value — that's a deployment-config refactor, not a code fix; would need its own plan
- Updating `PROJECT_OVERVIEW.md` to align docs with reality (the MinIO / 50MB / pgvector discrepancies flagged in the project analysis) — also a documentation PR

---

## Ready for user commit

7 files in the proposed PR (5 new, 2 modified):

```text
A  mediconyx-api/Swagger/FileUploadSchemaBuilder.cs
A  mediconyx-api/Swagger/FileUploadOperationFilter.cs
A  mediconyx-api/Tests/UnitTests/Swagger/FileUploadSchemaBuilderTests.cs
A  mediconyx-api/Models/DTOs/UploadFileRequest.cs
M  mediconyx-api/Controllers/FileAttachmentController.cs
M  mediconyx-api/Program.cs
M  mediconyx-api/Dockerfile
```

Suggested commit message:

```
fix(api): clean up Swagger / HTTPS / libgssapi noise

- Add FileUploadOperationFilter so Swashbuckle can document IFormFile
  endpoints in the OpenAPI spec (was 500 on /swagger/v1/swagger.json).
- Refactor FileAttachmentController.Upload to use an UploadFileRequest
  DTO — Swashbuckle's parameter discovery rejects [FromForm] IFormFile
  at discovery time, before any IOperationFilter runs.
- Gate UseHttpsRedirection() behind !DOTNET_RUNNING_IN_CONTAINER so
  containerized deployments don't log "Failed to determine the https
  port" (the dev container sets ASPNETCORE_ENVIRONMENT=Development,
  so IsDevelopment() can't be the gate).
- Install libgssapi-krb5-2 in the API Dockerfile final stage to
  satisfy Npgsql's startup GSSAPI probe (silences a noisy warning
  and future-proofs for Kerberos/SSPI auth).

Tests:
- +7 FileUploadSchemaBuilderTests (single/multi/empty input, parameter
  order, hashset projection, dedup).
- Existing 151 unit tests still pass (158 total, 1 skipped, 0 failed).
```

---

**Plan complete. Awaiting user commit per project convention.**