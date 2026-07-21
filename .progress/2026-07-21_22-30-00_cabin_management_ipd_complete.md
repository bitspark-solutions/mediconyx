# 2026-07-21 — Cabin Management Feature (IPD) Complete

## What was done

### Research
- Surveyed Bangladeshi private-hospital rate cards (Square, Popular, BRB, AMZ, BD Specialized) for cabin categories, amenities, and per-day BDT rates. Findings encoded as the `CabinTypes` catalog.

### Backend (mediconyx-api)
- **Design**: a cabin is a `Bed` with `Kind = Cabin` — admissions, transfers, bed-board, and per-bed-day billing all work for cabins with zero duplication.
- New files (one class per file, per user's standing rule):
  - `Models/BedKind.cs` — enum `Bed = 0, Cabin = 1`
  - `Models/CabinTypes.cs` — Shared/Standard/Deluxe/VIP/VVIP/Suite with default facilities + typical BDT rate ranges
  - `Models/BedStatus.cs` — extracted from Bed.cs
  - `Models/DTOs/CabinTypeInfoDto.cs`
- `Models/Bed.cs` — added `Kind`, `CabinType` (50), `Facilities` (JSON array, 1000)
- `Models/Ward.cs` — added `WardTypes.Cabin`
- `Data/ApplicationDbContext.cs` — Bed config: Kind conversion, CabinType/Facilities lengths, `(TenantId, Kind)` index
- `Models/DTOs/IpdDtos.cs` — BedDto/BedBoardBedDto/CreateBedRequest/UpdateBedStatusRequest carry cabin fields
- `Controllers/IPDController.cs` — cabin fields in all bed mappings; new endpoints:
  - `GET /api/ipd/cabins` (filters: status, cabinType)
  - `GET /api/ipd/cabin-types` (static catalog)
  - Create-bed validates cabin type, defaults facilities from catalog
- Migration `20260721000100_AddCabinSupport` (hand-written, repo style; includes `[DbContext]`+`[Migration]` attributes — without them EF silently ignores the migration, learned the hard way)
- **Side fix**: `ApplicationDbContextModelSnapshot.cs` had drifted behind several hand-written migrations; regenerated — now in sync with the model (large diff is the drift repair, not new schema).

### Frontend (mediconyx-ui)
- `app/portal/admin/bed-board/page.tsx`:
  - Cabin ward type (indigo color) in create-ward modal + board colors
  - Bed cards show cabin badge (type) + facility chips (full list in tooltip)
  - Create-bed modal: Ward Bed / Private Cabin toggle, cabin-type dropdown with typical rates, facility chips (toggle + custom add), defaults from `/api/ipd/cabin-types`
  - In Cabin wards the add button says "Cabin" and defaults to cabin creation

## Verification (test stack, localhost:9766)
- `dotnet build` clean; `tsc --noEmit` clean; **158/159 .NET tests pass** (1 skipped, pre-existing)
- Live smoke: cabin-types catalog ✅, create Deluxe (default facilities) ✅, create VIP (custom facilities) ✅, regular bed unchanged ✅, invalid type rejected ✅, `GET /cabins` ✅, admit patient to CAB-301 ✅, generate-bill → 1 day × 12,000 BDT ✅, bed-board shows kind/cabinType/Occupied ✅
- Migration applied on container start: `20260721000100_AddCabinSupport` in `__EFMigrationsHistory`

## Notes / follow-ups
- Dev stack (`docker-compose.yml`) still runs the pre-cabin image; rebuild when convenient.
- E2E suite (mediconyx-e2e) has no cabin coverage yet — candidate next task.
- Cabin rates bill through the existing per-bed-day `BedBillingService` (currency BDT).
