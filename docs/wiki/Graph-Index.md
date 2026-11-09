# Graph Index — community map

Subsystem → graphify community IDs (from GRAPH_REPORT.md, 316 communities).
Use as seed nodes for `graphify_query` / `graphify_context_bundle`.

## API — infrastructure & cross-cutting

| Community | Hub | What |
|---|---|---|
| 4 | `mediconyx_api.Data` | DbContext + DI wiring cluster |
| 8 | `ApplicationDbContext` | all DbSets (46 nodes) |
| 0 | `mediconyx_api.Migrations` | 65 migration nodes |
| 24 | `GlobalExceptionHandlerMiddleware` | middleware + correlation |
| 65 | `ServiceCollectionExtensions.cs` | DI registration, JWT, rate limit, JSON |
| 35 | `FileUploadOperationFilter` | swagger file upload schema |
| 36 | `IPasswordService` | password svc + BranchSeeder |
| 37/47 | `.GetUserId` / `.GetTenantId` | claims helpers + permission attrs |
| 46 | `mediconyx-api.csproj` | dependency manifest |

## API — tenancy & sales

| Community | Hub |
|---|---|
| 2 | `ITenantProvisioningService.ProvisionTenantAsync` |
| 9 | `SalesRequest` (entity + config) |
| 21/59/89 | `SalesRequestsController`, public create DTO, status/query |
| 17/41/60/78 | `Tenant`, `TenantSettingsDto`, controller, update req |
| 33/53 | `HospitalProfile` + response DTO |
| 74 | `TechnicalRequirements` |
| 67 | `ModuleSelection` (module toggles) |
| 25 | `OnboardingTimelineEvent` / `ApprovalController` |
| 5 | `BranchDto` (branches) |

## API — identity & clinical

| Community | Hub |
|---|---|
| 13/43/50 | `User`, `UserRole`, `Permission` |
| 73/64 | `Invitation`, `InvitationsController` |
| 48/16 | `DoctorProfile`, `PublicDoctorController` |
| 26/56/72/77 | `Patient`, self-registration, `CreatePatient`, `PatientDto` |
| 39/51/70 | `Appointment`, `AppointmentDto`, `AppointmentController` |
| 62/84/85 | `VitalSign`, `MedicalNoteController`, `PrescriptionController` |
| 75 | `TelehealthSession` |
| 52/57 | `FileAttachmentDto`, `StorageService` |

## API — IPD & billing

| Community | Hub |
|---|---|
| 76/86/55/91-UI | `Ward`, `Bed`, `BedBoardBedDto` |
| 38/44/58/80 | `Admission`, `AdmissionDto`, `AdmissionBillDto`, `BedBillingService` |
| 40/54/61 | `Invoice`, `InvoiceDto`, `InvoiceLineItem` |
| 27/49/66/42 | `Account`, `FiscalPeriod`, `JournalEntry`, `LedgerService` |
| 31/32/63/68 | journal DTOs, `BalanceSheetDto`, `IncomeStatementDto` |

## API — AI

| Community | Hub |
|---|---|
| 69 | `GenericEmbeddingService` |
| 83 | `GenerateChatCompletionAsync` / LLM DTOs |

## Frontend

| Community | Hub |
|---|---|
| 7/20 | `apiClient.ts` / `apiClient` |
| 10 | `useAuthStore` |
| 19 | `ProtectedRoute` (+ accountant/admin pages) |
| 14 | `master/page.tsx` (master portal) |
| 23 | `react` (accept-invite, doctor profile pages) |
| 91 | `bed-board/page.tsx` |
| 18 | `package.json` |

## Tests

| Community | Hub |
|---|---|
| 11/15/29/34/45 | SalesRequests, User, Invoice, Auth, Appointment suites |
| 81/82/88 | Patient, Password, UserManagement suites |

## Querying the graph

```bash
# via MCP tools (fast): graphify_query, graphify_node, graphify_neighbors,
#   graphify_community, graphify_core_hubs, graphify_shortest_path,
#   graphify_blast_radius, graphify_call_flow, graphify_dead_code, graphify_cypher
# project_path: "mediconyx"

# via CLI (needs MSYS_NO_PATHCONV on Git Bash):
MSYS_NO_PATHCONV=1 docker exec graphify-local-server \
  graphify query "how does admission billing work" \
  --graph /app/data/mediconyx/graphify-out/graph.json
```
