# Storage Service

.NET 8 minimal API, own Docker container, host ports 8890 (dev) / 8891 (test)
— moved off lower ports because Windows Hyper-V reserves 7871–7970.

## Features

- **Unified upload policy** (both API & storage enforce): PDF, JPEG, PNG, GIF,
  DICOM; ≤50MB.
- **Auth**: `X-Api-Key` header. Dev default is empty = open with a logged warning;
  set `STORAGE_API_KEY` in `.env` for anything real.
- **Signed download URLs** — real HMAC tokens with expiry, not obfuscation.
- **Soft deletes** — `SoftDeletePurgeService` permanently purges after 30 days.

## API-side integration

`IStorageService` / `StorageService` (graph community 57) in
`mediconyx-api/Services/` calls it over `StorageService__BaseUrl`
(`http://storage:8080` inside the compose network) with
`StorageService__ApiKey`.

`FileAttachment` entities store metadata in Postgres; blobs live in the storage
volume. Attachments hang off appointments, patient documents, hospital profile
docs (license, tax cert, business registration).

## Gotcha that bit us

API-side SHA-256 dedup discards identical-content uploads — three fixture PDFs
with the same contents collapse into one attachment (fixed 2026-08-29 by giving
each fixture unique content). If a test uploads "different files," they must
actually differ in bytes.
