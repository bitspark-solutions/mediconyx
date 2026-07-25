# 2026-07-25 — Storage Bug Batch: auth, policy, purge, signed URLs (+3 hidden bugs)

Regression: **170/171 unit, 150 E2E passed, 0 failed.** Verified end-to-end: GIF upload → API → storage auth → DB → download.

## The four known bugs (fixed)

1. **Upload-policy mismatch** — one policy both sides: PDF/JPEG/PNG/GIF/DICOM, **50MB** (was: API allowed GIF/DICOM/octet-stream + 500MB, storage rejected them → 500). Storage now sniffs GIF87a/89a and the DICOM `DICM`@128 preamble; `application/octet-stream` dropped everywhere. (Huge >50MB scans = future chunked-upload feature.)
2. **Storage had no auth** — all `/api/*` now require `X-Api-Key` (401 otherwise; empty key = dev mode with loud startup warning). Key generated into `.env` as `STORAGE_API_KEY`, wired via both compose files; API client (`Services/StorageService.cs`) sends it automatically.
3. **Soft-deletes never purged** — new `SoftDeletePurgeService` (hosted): every `Storage:PurgeIntervalHours` (6h) hard-deletes blob + sidecar for files soft-deleted longer than `SoftDeleteRetentionDays` (30).
4. **Fake signed URLs** — real HMAC-SHA256 tokens (base64url `fileId|expiry|hmac`, constant-time compare) via `UrlSigningService`; download returns 403 for invalid/expired tokens; valid tokens bypass the API-key requirement (direct browser downloads); no token without key → 401.

## Three hidden bugs found during verification (fixed)

5. **Kebab-case routes never worked**: `[Route("api/[controller]")]` produces `/api/FileAttachment`, but UI/docs call `/api/file-attachment` → 404 since forever. Same for `vital-sign`, `medical-note`, `doctor-schedule` — all four controllers now have explicit kebab-case routes. (E2E never caught it: no upload coverage.)
6. **`FileAttachments`/`AIConversations`/`AIMessages` had no migration** — they predate the migration set (created via EnsureCreated on old DBs). Any migrated/fresh DB lacked them → upload 500ed at DB save. New migration `20260721000400_AddAiRagTables` creates all three per the snapshot schema.
7. **Windows Hyper-V port exclusion** (7871–7970) blocked storage containers from binding 7890/7891 after recreate — host ports remapped to **8890/8891** in both compose files (inter-container traffic on :8080 unaffected).

## Notes
- `appsettings.Development.json` still points local `dotnet run` storage calls at `localhost:7890` — update if you run outside Docker.
- Verified on test stack: no-key 401, keyed GIF 201, signed URL 200 / tampered 403 / key-no-token 200 / neither 401, fake-PDF magic-number 400; and on dev: full upload→download→list chain.
