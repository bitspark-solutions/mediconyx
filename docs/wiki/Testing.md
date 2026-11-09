# Testing

## Unit / integration tests (`mediconyx-api/Tests`)

xunit + FluentAssertions + Moq + coverlet.

```bash
"C:/Program Files/dotnet/dotnet.exe" test mediconyx-api/Tests/mediconyx-api.Tests.csproj
# expect ~176 passed / 1 skipped
```

Per-area suites visible in the graph: `SalesRequestsControllerTests` (43 edges —
most-tested surface), `UserServiceTests`, `AuthServiceTests`, `InvoiceServiceTests`,
`AppointmentServiceTests`, `PatientServiceTests`, `PasswordServiceTests`,
`DoctorControllerTests`, `UserManagementControllerTests`, plus swagger smoke tests.

## E2E (`../mediconyx-e2e`, Playwright)

Test stack must be up: `docker compose -f docker-compose.test.yml up -d`.

```bash
cd ../mediconyx-e2e
npm run test:env              # full suite — last green: 153 pass / 0 fail / 4 skip
npm run test:onboarding       # API-driven onboarding story
npm run test:onboarding:ui    # browser-driven onboarding story
npm run test:onboarding:slow  # headed + slow-mo → demo video
node scripts/collect-videos.mjs <desc>   # collect MP4s → test-videos/<datetime>_<desc>/
```

Rules:

- **Tests self-provision their data** — must pass on a fresh volume. Codes ≤20 chars.
- Browser tests record video always on the `e2e` project, on-failure for `ui`.
- `uiLogin` clears prior sessions before logging in (AuthRedirectGuard gotcha).

## Coverage gaps (from RESUME backlog)

- File upload/download chain — works, no E2E coverage (hid 2 bugs already — high value).
- UI-level E2E for employees page + bed-board cabins (API layer covered).
- Cross-branch isolation E2E — part of multi-branch Stage 4.
