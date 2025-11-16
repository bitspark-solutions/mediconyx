## Likely Causes
- Missing/late Authorization header (store not hydrated before fetch)
- Expired/missing JWT; no automatic refresh
- Role mismatch in token (UI expects Sales; API requires Sales/Master)

## Proposed Fix
1. Create a shared `apiClient.ts`:
- Reads `accessToken` from `useAuthStore`
- Attaches `Authorization: Bearer <token>` to every request
- On 401, calls `/api/auth/refresh` with `refreshToken`, updates store, and retries once
- On repeated failure, clears auth and redirects to `/login`

2. Update Sales Dashboard data calls:
- Replace `fetch(...)` in `portal/sales` (KPIs, Revenue, Requests) with `apiClient.get(...)`
- Ensure effects wait for store hydration (`accessToken` readiness) before firing

3. Store hardening:
- Ensure `useAuthStore` persists tokens and normalizes `user.role`
- After `login`, fetch `/api/UserManagement/me` to confirm role; set `user.role` accordingly

4. API validation:
- Verify JWT includes `ClaimTypes.Role` and NameIdentifier; endpoints use `[Authorize(Roles = "Sales,Master")]`
- Confirm CORS policy allows `Authorization` header

5. Testing
- Clear local storage; login as Sales; verify request headers include `Authorization`
- Validate KPIs/Revenue/Requests load; simulate token expiry to ensure refresh/retry works

## Deliverables
- `apiClient.ts` with Auth header + refresh logic
- Sales dashboard updated to use client and guard fetch until token ready
- Verified metrics/requests load without 401 and recover on token expiry