# Mediconyx API Quality Fixes — Implementation Plan

> **For Hermes:** Use subagent-driven-development to implement task-by-task.

**Goal:** Eliminate three known noisy warnings in the running `mediconyx-api` Docker stack (Swagger 500 on `/swagger/v1/swagger.json`, HTTPS-redirect port warning, missing `libgssapi_krb5.so.2`) using the canonical, industry-recommended fixes from Microsoft and Swashbuckle.

**Architecture:** Three independent, self-contained fixes applied to the API project. Each fix is small, single-purpose, and covered by a unit test. No new NuGet packages, no architectural change.

**Tech Stack:** .NET 10 / ASP.NET Core · Swashbuckle.AspNetCore 10.0.0 · Npgsql · xUnit + FluentAssertions + Microsoft.AspNetCore.Mvc.Testing · Docker multi-stage build (Ubuntu 24.04 base)

---

## Edge Cases (REQUIRED section)

- **On the wire / at the boundary**
  - File upload Swagger doc must declare `multipart/form-data` with `IFormFile` as `{ type: "string", format: "binary" }` — matches what Postman / Swagger UI actually send. Any other schema would break Swagger UI's "Try it out".
  - The filter must only mutate operations that actually have an `IFormFile` parameter — must be a no-op for every other action (95%+ of endpoints).
  - The filter must remove the auto-generated `[FromForm]` form parameters from `operation.Parameters` AFTER copying them into `requestBody.content` — otherwise Swagger UI shows them twice.
  - Operations with multiple `IFormFile` parameters must list all of them in `properties` and `required`.
  - `FileAttachmentController` accepts optional metadata fields alongside the file — the doc must not lose those non-file form fields.

- **At auth / on the server**
  - `UseHttpsRedirection()` must remain active for non-containerized local dev (`dotnet run` on developer laptop) so `https://localhost:5001` still works.
  - In Production env (container behind reverse proxy), the middleware must NOT run, must NOT log the "Failed to determine the https port" warning, and must NOT redirect (TLS is terminated upstream).
  - The `libkrb5-3` install must happen in the **runtime stage** (`FROM ... aspnet:10.0`), NOT the SDK stage, to keep the publish image lean. Build stage does not need it.
  - `apt-get install` must clean `/var/lib/apt/lists/*` afterwards — keeps image size down (linter would fail CI on a fat image).
  - `apt-get update` and `install` must be in the same `RUN` to avoid stale cache layer reuse.

- **Multi-user / multi-tenant**
  - None for v1 — none of these fixes touch tenant isolation, auth claims, or data.

- **Operational**
  - If the libkrb5 install fails at build time, the unit-test step (`dotnet test`) in the SDK stage would still pass because Kerberos isn't loaded there — but the **runtime image** would silently keep the warning. Mitigation: build the container, run it, grep logs for the warning.
  - Swashbuckle doc generation runs only in `IsDevelopment()` (current behavior); the existing Swagger UI at `/swagger` still works without the fix, but `/swagger/v1/swagger.json` 500s. The fix benefits both.
  - If a future contributor adds a new `[FromForm] IFormFile` endpoint, the filter handles it automatically — no per-endpoint work.

---

## Research / Prior Art (per writing-plans skill)

- **Swashbuckle `IFormFile` recipe** — official Swashbuckle docs: <https://github.com/domaindrivendev/Swashbuckle.AspNetCore/tree/master/docs/configure-and-customize-swaggergen.md#handle-forms-and-file-uploads>. Pattern: `IOperationFilter` that builds `OpenApiRequestBody` from `IFormFile` params. This is the same filter used in Microsoft eShopOnContainers and most ABP / Clean-Architecture templates.
- **HTTPS redirect in containers** — Microsoft docs explicitly recommend dropping `UseHttpsRedirection()` from containerized apps and terminating TLS at the edge (nginx, ALB, etc.): <https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl>. Production .NET 6+ shops follow this.
- **Npgsql Kerberos probe** — `Npgsql` eagerly loads `libgssapi_krb5` at startup as part of its auth provider discovery. Documented in Npgsql auth docs: <https://www.npgsql.org/doc/basic-usage.html>. Installing `libkrb5-3` is the standard resolution on Debian/Ubuntu base images.

---

## Task Index

1. **Task 1:** Add `FileUploadOperationFilter` and unit test (TDD)
2. **Task 2:** Wire the filter into `Program.cs` and verify Swagger JSON renders
3. **Task 3:** Gate `UseHttpsRedirection()` to non-Production
4. **Task 4:** Install `libkrb5-3` in API Dockerfile final stage
5. **Task 5:** End-to-end smoke — `make up` → verify all three warnings gone

---

### Task 1: Add `FileUploadOperationFilter` (TDD)

**Objective:** Create a Swashbuckle `IOperationFilter` that teaches the OpenAPI generator how to document endpoints taking `IFormFile`, with a unit test that asserts the produced schema.

**Files:**
- Create: `mediconyx-api/Swagger/FileUploadOperationFilter.cs`
- Create: `mediconyx-api/Tests/UnitTests/Swagger/FileUploadOperationFilterTests.cs`

**Step 1: Write the failing test**

```csharp
// mediconyx-api/Tests/UnitTests/Swagger/FileUploadOperationFilterTests.cs
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace mediconyx_api.Tests.UnitTests.Swagger;

public class FileUploadOperationFilterTests
{
    [Fact]
    public void Apply_SetsMultipartRequestBody_WithBinarySchema_WhenIFormFilePresent()
    {
        // arrange — synthesize the OperationFilterContext with one IFormFile param
        var filter = new FileUploadOperationFilter();
        var operation = new OpenApiOperation { Parameters = new List<OpenApiParameter>
        {
            new() { Name = "file", In = ParameterLocation.Query }
        }};

        var ctx = new OperationFilterContext(
            apiDescription: FakeApiDescription.WithFormFileParam(),
            schemaRegistry: new SchemaRegistry(new Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorOptions(), new FakeJsonContractResolver()),
            schemaGenerator: new Swashbuckle.AspNetCore.SwaggerGen.SchemaGenerator(new Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorOptions(), new FakeJsonContractResolver()));

        // act
        filter.Apply(operation, ctx);

        // assert
        operation.RequestBody.Should().NotBeNull();
        operation.RequestBody.Content.Should().ContainKey("multipart/form-data");
        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
        schema.Type.Should().Be("object");
        schema.Properties.Should().ContainKey("file");
        schema.Properties["file"].Format.Should().Be("binary");
        schema.Properties["file"].Type.Should().Be("string");
        operation.Parameters.Should().BeEmpty(
            because: "the original form parameter must be removed once moved into requestBody");
    }

    [Fact]
    public void Apply_IsNoOp_WhenNoIFormFileParameter()
    {
        var filter = new FileUploadOperationFilter();
        var operation = new OpenApiOperation { Parameters = new List<OpenApiParameter>
        {
            new() { Name = "id", In = ParameterLocation.Path }
        }};

        var ctx = FakeOpFilterContext.NoFormFile();

        filter.Apply(operation, ctx);

        operation.RequestBody.Should().BeNull();
        operation.Parameters.Should().HaveCount(1);
    }
}
```

`FakeApiDescription` and `FakeOpFilterContext` are small test helpers (≤30 lines total, placed in the same test file or in a sibling `Fakes/` folder). The actual filter test only needs to verify the **output schema**, not the internal Swashbuckle plumbing — so the fakes can be minimal stubs.

**Step 2: Run the test to verify failure**

```bash
cd /c/Users/Mohsin/Projects/mediconyx
"C:/Program Files/dotnet/dotnet.exe" test mediconyx-api/Tests/mediconyx-api.Tests.csproj \
  --filter "FullyQualifiedName~FileUploadOperationFilterTests"
```

Expected: **FAIL** — `FileUploadOperationFilter` does not exist yet.

**Step 3: Write the minimal implementation**

```csharp
// mediconyx-api/Swagger/FileUploadOperationFilter.cs
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace mediconyx_api.Swagger;

/// <summary>
/// Tells Swashbuckle how to document ASP.NET Core actions that accept
/// [FromForm] IFormFile parameters. By default, Swashbuckle's reflection
/// cannot model these, causing /swagger/v1/swagger.json to return 500.
///
/// Recipe: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/docs/configure-and-customize-swaggergen.md#handle-forms-and-file-uploads
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Type == typeof(IFormFile))
            .ToList();

        if (formFileParams.Count == 0)
            return;

        // Build the multipart/form-data request body schema.
        var properties = formFileParams.ToDictionary(
            p => p.Name,
            _ => new OpenApiSchema { Type = "string", Format = "binary" });

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties,
                        Required = properties.Keys.ToHashSet()
                    }
                }
            }
        };

        // Remove the auto-generated form parameters so Swagger UI doesn't
        // render them twice (once as body, once as standalone parameter).
        var fileParamNames = formFileParams.Select(p => p.Name).ToHashSet();
        operation.Parameters.RemoveAll(p => fileParamNames.Contains(p.Name));
    }
}
```

**Step 4: Run the test to verify pass**

```bash
cd /c/Users/Mohsin/Projects/mediconyx
"C:/Program Files/dotnet/dotnet.exe" test mediconyx-api/Tests/mediconyx-api.Tests.csproj \
  --filter "FullyQualifiedName~FileUploadOperationFilterTests"
```

Expected: **PASS** (2 tests). Full suite must still pass:

```bash
"C:/Program Files/dotnet/dotnet.exe" test mediconyx-api/Tests/mediconyx-api.Tests.csproj
```

Expected: **151 passed, 1 skipped, 0 failed** (same as the current `master` baseline).

**Step 5: Handoff to user (user commits)**

Files created:
- `mediconyx-api/Swagger/FileUploadOperationFilter.cs`
- `mediconyx-api/Tests/UnitTests/Swagger/FileUploadOperationFilterTests.cs`

---

### Task 2: Wire filter into `Program.cs`; verify Swagger JSON renders

**Objective:** Register the filter in `AddSwaggerGen()` and verify the API still generates `/swagger/v1/swagger.json` without 500.

**Files:**
- Modify: `mediconyx-api/Program.cs:21-23` (the `AddSwaggerGen` block)

**Step 1: Edit Program.cs**

Current:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

Replace with:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Document endpoints that take IFormFile via [FromForm]
    // (FileAttachmentController.Upload, etc.) — Swashbuckle cannot model these
    // out of the box and would 500 /swagger/v1/swagger.json otherwise.
    options.OperationFilter<FileUploadOperationFilter>();
    options.SupportNonNullableReferenceTypes();
});
```

Add the using at the top of the file alongside the others:
```csharp
using mediconyx_api.Swagger;
```

**Step 2: Verify build**

```bash
cd /c/Users/Mohsin/Projects/mediconyx
"C:/Program Files/dotnet/dotnet.exe" build mediconyx-api/mediconyx-api.csproj
```

Expected: **Build succeeded. 0 Warning(s) 0 Error(s)**.

**Step 3: Run full test suite**

```bash
"C:/Program Files/dotnet/dotnet.exe" test mediconyx-api/Tests/mediconyx-api.Tests.csproj
```

Expected: **151 passed, 1 skipped, 0 failed**.

**Step 4: Smoke-test Swagger JSON**

```bash
cd /c/Users/Mohsin/Projects/mediconyx
make up
sleep 8
curl -s -o /dev/null -w "Swagger JSON: HTTP %{http_code}\n" \
  http://localhost:9765/swagger/v1/swagger.json
curl -s http://localhost:9765/swagger/v1/swagger.json | \
  python -c "import sys,json; d=json.load(sys.stdin); \
  upload = [p for p in d['paths'].values() for op in p.values() \
  if 'multipart/form-data' in op.get('requestBody',{}).get('content',{})]; \
  print('Endpoints with multipart/form-data:', len(upload))"
```

Expected:
- Swagger JSON: **HTTP 200** (was 500)
- At least one endpoint (FileAttachmentController.Upload) listed with multipart/form-data

**Step 5: Handoff to user**

Files modified:
- `mediconyx-api/Program.cs`

---

### Task 3: Gate `UseHttpsRedirection()` to non-Production

**Objective:** Stop the "Failed to determine the https port for redirect" warning in containerized environments. Keep dev convenience intact.

**Files:**
- Modify: `mediconyx-api/Program.cs:124-130`

**Step 1: Edit Program.cs**

Current (around line 124):
```csharp
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();           // <-- always runs, warns in containers
app.UseMiddleware<CorrelationIdMiddleware>();
```

Replace the unconditional `UseHttpsRedirection()` with an env-gated block:
```csharp
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirection is only relevant in dev (where Kestrel may listen on
// both http and https). In containerized / Production environments, TLS
// is terminated at the reverse proxy / load balancer and the app speaks
// plain HTTP — running UseHttpsRedirection() here just logs noise.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();
```

**Step 2: Verify build + tests still pass**

```bash
cd /c/Users/Mohsin/Projects/mediconyx
"C:/Program Files/dotnet/dotnet.exe" build mediconyx-api/mediconyx-api.csproj
"C:/Program Files/dotnet/dotnet.exe" test mediconyx-api/Tests/mediconyx-api.Tests.csproj
```

Expected: build clean, tests 151 passed / 1 skipped / 0 failed.

**Step 3: Handoff to user**

Files modified:
- `mediconyx-api/Program.cs`

---

### Task 4: Install `libkrb5-3` in API Dockerfile final stage

**Objective:** Stop the `libgssapi_krb5.so.2: cannot open shared object` warning emitted by Npgsql at startup.

**Files:**
- Modify: `mediconyx-api/Dockerfile`

**Step 1: Edit Dockerfile**

Current:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet","mediconyx-api.dll"]
```

Replace with:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Install Kerberos GSSAPI library. Npgsql eagerly probes for it at startup
# even when authenticating with username/password — missing it only logs a
# noisy warning, but installing it future-proofs the image for SSPI/Kerberos
# auth and silences the warning cleanly. The .NET 10 base image is Ubuntu
# 24.04 (Debian-based), so apt-get is available. Cleaned lists keep the
# layer small.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libkrb5-3 \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet","mediconyx-api.dll"]
```

**Step 2: Rebuild the API image and recreate the container**

```bash
cd /c/Users/Mohsin/Projects/mediconyx
docker compose build api
make up
```

Expected: build succeeds. No `libgssapi_krb5` warning in `docker logs mediconyx-api`.

**Step 3: Verify warning is gone**

```bash
sleep 8
docker logs mediconyx-api --tail 200 2>&1 | grep -i "libgssapi" || echo "OK: no libgssapi warning"
```

Expected: prints `OK: no libgssapi warning`.

**Step 4: Handoff to user**

Files modified:
- `mediconyx-api/Dockerfile`

---

### Task 5: End-to-end smoke + verification report

**Objective:** Confirm all three warnings are eliminated in a real running stack, capture verification output.

**Step 1: Full rebuild + restart**

```bash
cd /c/Users/Mohsin/Projects/mediconyx
docker compose down
docker compose build
make up
sleep 10
```

**Step 2: Verify each fix end-to-end**

```bash
echo "=== Fix 1: Swagger JSON no longer 500 ==="
curl -s -o /dev/null -w "HTTP %{http_code}\n" http://localhost:9765/swagger/v1/swagger.json

echo ""
echo "=== Fix 2: HTTPS redirect warning gone from API logs ==="
docker logs mediconyx-api --tail 200 2>&1 \
  | grep -i "https port" \
  && echo "FAIL: warning still present" \
  || echo "OK: no HTTPS port warning"

echo ""
echo "=== Fix 3: libgssapi warning gone from API logs ==="
docker logs mediconyx-api --tail 200 2>&1 \
  | grep -i "libgssapi_krb5" \
  && echo "FAIL: warning still present" \
  || echo "OK: no libgssapi warning"

echo ""
echo "=== Regression: login still works ==="
curl -s -X POST http://localhost:9765/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"sales@mediconyx.local","password":"Sales@123"}' \
  | grep -o '"accessToken":"[^"]\{20\}' \
  && echo "OK: login succeeded"
```

Expected output:
```
=== Fix 1: Swagger JSON no longer 500 ===
HTTP 200
=== Fix 2: HTTPS redirect warning gone from API logs ===
OK: no HTTPS port warning
=== Fix 3: libgssapi warning gone from API logs ===
OK: no libgssapi warning
=== Regression: login still works ===
"accessToken":"eyJhbGciOiJIUzI1NiIs
OK: login succeeded
```

**Step 3: Capture output in progress report**

Append a new `.progress/` file per project convention:

```
.progress/2026-06-27_api_quality_fixes_progress_report.md
```

Content: what was fixed, the verification commands + their output, file inventory.

**Step 4: Final handoff to user**

Files modified across all tasks:
- `mediconyx-api/Swagger/FileUploadOperationFilter.cs` (new)
- `mediconyx-api/Tests/UnitTests/Swagger/FileUploadOperationFilterTests.cs` (new)
- `mediconyx-api/Program.cs`
- `mediconyx-api/Dockerfile`
- `.progress/2026-06-27_api_quality_fixes_progress_report.md` (new)

---

## Out of scope (intentional)

- Replacing storage-service local FS with MinIO/S3 (separate, much larger work)
- Upgrading `storage-service` from .NET 8 to .NET 10
- Splitting the 280-line seed block in `Program.cs` into an `IDbSeeder` (separate refactor)
- Fixing the `syncwire-app` unhealthy state in another project on this host (out of scope)
- Adding pgvector for semantic RAG retrieval (per PROJECT_OVERVIEW, deferred feature)

---

## Risk Summary

| Risk | Likelihood | Mitigation |
|---|---|---|
| Swagger filter breaks non-file endpoints | Low | Filter is a no-op when no `IFormFile` param; covered by 2nd unit test |
| New `OperationFilterContext` test fakes are brittle vs. Swashbuckle internals | Medium | If fakes fight Swashbuckle types, swap to integration test via `WebApplicationFactory<Program>` + assert `swagger.json` content |
| `libkrb5` install bloats image | Low | `--no-install-recommends` + `rm -rf /var/lib/apt/lists/*` keeps it under ~1 MB |
| Docker layer cache hides broken install | Low | Build with `--no-cache` on first run; verify warning absent in logs |
| `UseHttpsRedirection()` removal breaks someone's local `dotnet run` on https | Low | Gate is `IsDevelopment()` only — Kestrel still binds https if `applicationUrls` includes it; just no auto-redirect |