# Frontend (`mediconyx-ui`)

Next.js 16 + React 19 + Tailwind 4 + Zustand + chart.js + lucide-react.

## Portals (role-switched after login)

From graph communities 10, 14, 19, 20 — the app is organized as role portals:

- **Master portal** (`master/page.tsx`) — platform admin: tenants tab, users tab,
  hospital settings tab, module labels, appointment requests dashboard
- **Admin portal** (`/portal/admin/...`) — branches page + branch modal, employees
  (incl. branch field + custom designations), hospital profile, settings
- **Receptionist portal** — appointment booking, slots, patient registration
- **Accountant portal** — invoices tab, record-payment tab
- **Doctor / Patient portals** — schedules, notes, prescriptions; patient self-service

## Key infrastructure

| Piece | What |
|---|---|
| `apiClient.ts` / `apiClient` | central API client — every portal flows through it (communities 7, 20) |
| `useAuthStore` | Zustand auth store — token, user, role → portal switch (community 10) |
| `ProtectedRoute` | route guard; `AuthRedirectGuard` bounces logged-in users to their portal (19) |
| `bed-board/page.tsx` | bed board — `BedCard`, `AdmitModal`, `BillModal`, branch filter (91) |
| `FileUploadOperationFilter` (API side) | swagger file-upload schema |

## Gotchas

- `NEXT_PUBLIC_API_BASE_URL` is **build-time inlined** — changing it needs
  `docker compose build ui`, not a restart.
- `uiLogin` (E2E helper) must clear previous sessions or `AuthRedirectGuard`
  redirects before the new login completes — burned us on 2026-08-29.
- Typecheck: `cd mediconyx-ui && npx tsc --noEmit`
