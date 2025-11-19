# Hospital Onboarding Process - Implementation Tasks

## Phase 1: Database Schema & Models (Backend Foundation)

### [ ] TASK-001: Extend SalesRequest Status Enum
**Priority**: High  
**Effort**: 30min  
**Dependencies**: None

- [ ] Add new status values to SalesRequestStatus enum: Qualified, AwaitingPayment, Provisioning, Rejected, Suspended
- [ ] Update existing status handling in controllers to support new values
- [ ] Add migration for enum extension
- [ ] Update API documentation

**Files to modify**:
- `mediconyx-api/Models/SalesRequest.cs`

---

### [ ] TASK-002: Create HospitalProfile Entity
**Priority**: High  
**Effort**: 2h  
**Dependencies**: TASK-001

- [ ] Create HospitalProfile model with all required fields (legal name, license, tax ID, etc.)
- [ ] Add HospitalType enum (Public, Private, Specialized, Clinic)
- [ ] Configure 1:1 relationship with SalesRequest
- [ ] Add validation attributes (StringLength, Required, RegularExpression)
- [ ] Add to DbContext

**Files to create**:
- `mediconyx-api/Models/HospitalProfile.cs`

**Files to modify**:
- `mediconyx-api/Data/ApplicationDbContext.cs`

---

### [ ] TASK-003: Create TechnicalRequirements Entity
**Priority**: High  
**Effort**: 1.5h  
**Dependencies**: TASK-001

- [ ] Create TechnicalRequirements model with user counts, departments, etc.
- [ ] Configure 1:1 relationship with SalesRequest
- [ ] Add validation attributes
- [ ] Add to DbContext

**Files to create**:
- `mediconyx-api/Models/TechnicalRequirements.cs`

**Files to modify**:
- `mediconyx-api/Data/ApplicationDbContext.cs`

---

### [ ] TASK-004: Create ModuleSelection Entity
**Priority**: High  
**Effort**: 1h  
**Dependencies**: TASK-001

- [ ] Create ModuleSelection model with boolean flags for each module
- [ ] Add ModuleType enum (OPD, IPD, Emergency, Pharmacy, etc.)
- [ ] Configure 1:1 relationship with SalesRequest
- [ ] Add to DbContext

**Files to create**:
- `mediconyx-api/Models/ModuleSelection.cs`

**Files to modify**:
- `mediconyx-api/Data/ApplicationDbContext.cs`

---

### [ ] TASK-005: Extend SalesRequest Entity for Subscription
**Priority**: High  
**Effort**: 1h  
**Dependencies**: TASK-001

- [ ] Add subscription fields: ContractStartDate, ContractDuration, AutoRenewal, UserLicenses
- [ ] Add payment fields: PaymentMethod enum, PaymentTerms enum, DiscountApplied
- [ ] Add SpecialTerms text field
- [ ] Add TenantId nullable foreign key
- [ ] Update existing SalesRequest model

**Files to modify**:
- `mediconyx-api/Models/SalesRequest.cs`

---

### [ ] TASK-006: Create DocumentType Enum and Update Attachments
**Priority**: High  
**Effort**: 45min  
**Dependencies**: None

- [ ] Create DocumentType enum (License, TaxCert, BusinessReg, DirectorID, etc.)
- [ ] Add DocumentType field to SalesRequestAttachment
- [ ] Add IsMandatory flag
- [ ] Add MaxFileSizeBytes field

**Files to modify**:
- `mediconyx-api/Models/SalesRequestAttachment.cs`

---

### [ ] TASK-007: Create Database Migration
**Priority**: High  
**Effort**: 1h  
**Dependencies**: TASK-002, TASK-003, TASK-004, TASK-005, TASK-006

- [ ] Generate EF Core migration for all new entities
- [ ] Review migration SQL for correctness
- [ ] Add indexes: (Status, CreatedAt), OwnerSalesUserId, TenantId
- [ ] Test migration up/down

**Commands**:
```bash
docker run --rm -v /Users/mohsin/projects/mediconyx/mediconyx-api:/src -w /src --network mediconyx_default mcr.microsoft.com/dotnet/sdk:10.0 bash -c "dotnet restore && dotnet tool install --global dotnet-ef && export PATH=\$PATH:/root/.dotnet/tools && dotnet ef migrations add HospitalOnboardingSchema"
```

---

## Phase 2: Storage Service Implementation

### [ ] TASK-008: Create Storage Service Project
**Priority**: High  
**Effort**: 2h  
**Dependencies**: None

- [ ] Create new ASP.NET Core Minimal API project
- [ ] Configure port 7990 in launchSettings.json
- [ ] Add Dockerfile with multi-stage build
- [ ] Add to docker-compose.yml with storage volume
- [ ] Configure CORS for mediconyx-api access

**Files to create**:
- `storage-service/Program.cs`
- `storage-service/Dockerfile`
- `storage-service/storage-service.csproj`

**Files to modify**:
- `docker-compose.yml`

---

### [ ] TASK-009: Implement Storage Provider Interface
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-008

- [ ] Create IStorageProvider interface with Upload, Download, Delete, GetMetadata, Exists, GetUrl methods
- [ ] Create StorageResult and FileMetadata DTOs
- [ ] Create LocalFileSystemProvider implementation
- [ ] Add configuration options for storage settings
- [ ] Implement file path generation (year/month/day structure)

**Files to create**:
- `storage-service/Interfaces/IStorageProvider.cs`
- `storage-service/Models/StorageResult.cs`
- `storage-service/Models/FileMetadata.cs`
- `storage-service/Providers/LocalFileSystemProvider.cs`
- `storage-service/Configuration/StorageOptions.cs`

---

### [ ] TASK-010: Implement Storage Service API Endpoints
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-009

- [ ] POST /api/files/upload - multipart file upload
- [ ] GET /api/files/{fileId}/download - file download with content-type
- [ ] GET /api/files/{fileId}/url - generate temporary signed URL
- [ ] DELETE /api/files/{fileId} - soft delete file
- [ ] GET /api/files/{fileId}/metadata - file information
- [ ] Add service-to-service JWT authentication middleware

**Files to create**:
- `storage-service/Endpoints/FilesEndpoints.cs`
- `storage-service/Middleware/ServiceAuthMiddleware.cs`

---

### [ ] TASK-011: Add File Validation to Storage Service
**Priority**: High  
**Effort**: 2h  
**Dependencies**: TASK-010

- [ ] Implement magic number validation (verify file type)
- [ ] Add file size limit enforcement
- [ ] Implement SHA256 hash calculation
- [ ] Add virus scanning stub (ClamAV integration point)
- [ ] Create validation result DTO

**Files to create**:
- `storage-service/Services/FileValidationService.cs`
- `storage-service/Services/IFileValidationService.cs`

---

### [ ] TASK-012: Build and Test Storage Service
**Priority**: High  
**Effort**: 1h  
**Dependencies**: TASK-011

- [ ] Build storage service Docker image
- [ ] Start storage service container
- [ ] Test file upload endpoint with curl
- [ ] Test file download endpoint
- [ ] Verify storage volume persistence
- [ ] Check logs for errors

**Commands**:
```bash
docker-compose build storage
docker-compose up -d storage
curl -X POST -F "file=@test.pdf" http://localhost:7990/api/files/upload
```

---

## Phase 3: Backend API Extensions

### [ ] TASK-013: Create HospitalProfile DTOs
**Priority**: High  
**Effort**: 1h  
**Dependencies**: TASK-002

- [ ] Create HospitalProfileCreateDto
- [ ] Create HospitalProfileUpdateDto
- [ ] Create HospitalProfileResponseDto
- [ ] Add AutoMapper profiles for mapping

**Files to create**:
- `mediconyx-api/Models/DTOs/HospitalProfileDtos.cs`

---

### [ ] TASK-014: Create HospitalProfile Controller Endpoints
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-013, TASK-007

- [ ] POST /api/sales/requests/{id}/hospital-profile - create/update profile
- [ ] GET /api/sales/requests/{id}/hospital-profile - retrieve profile
- [ ] Add validation logic
- [ ] Create timeline event on submission
- [ ] Update SalesRequest status to InProgress if Qualified
- [ ] Add authorization (Sales, Admin, Master roles)

**Files to create**:
- `mediconyx-api/Controllers/HospitalProfileController.cs`

---

### [ ] TASK-015: Create TechnicalRequirements Controller Endpoints
**Priority**: High  
**Effort**: 2h  
**Dependencies**: TASK-003, TASK-007

- [ ] POST /api/sales/requests/{id}/technical-requirements
- [ ] GET /api/sales/requests/{id}/technical-requirements
- [ ] Add validation logic
- [ ] Create timeline event on submission

**Files to create**:
- `mediconyx-api/Controllers/TechnicalRequirementsController.cs`

---

### [ ] TASK-016: Create ModuleSelection Controller Endpoints
**Priority**: Medium  
**Effort**: 2h  
**Dependencies**: TASK-004, TASK-007

- [ ] POST /api/sales/requests/{id}/modules - update module selection
- [ ] GET /api/sales/requests/{id}/modules - retrieve selected modules
- [ ] Add default module sets per subscription plan

**Files to create**:
- `mediconyx-api/Controllers/ModuleSelectionController.cs`

---

### [ ] TASK-017: Extend SalesRequests Controller for New Statuses
**Priority**: High  
**Effort**: 2h  
**Dependencies**: TASK-001, TASK-007

- [ ] Add PATCH /api/sales/requests/{id}/qualify endpoint (Pending → Qualified)
- [ ] Add PATCH /api/sales/requests/{id}/reject endpoint (any → Rejected with reason)
- [ ] Add PATCH /api/sales/requests/{id}/suspend endpoint (Active → Suspended)
- [ ] Add PATCH /api/sales/requests/{id}/reactivate endpoint (Suspended → Active)
- [ ] Update status validation logic for all transitions
- [ ] Add reason field requirement for reject/suspend

**Files to modify**:
- `mediconyx-api/Controllers/SalesRequestsController.cs`
- `mediconyx-api/Models/DTOs/SalesDtos.cs`

---

### [ ] TASK-018: Integrate Storage Service Client
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-012

- [ ] Create IStorageServiceClient interface
- [ ] Implement HttpClient-based StorageServiceClient
- [ ] Add retry logic with Polly
- [ ] Add service-to-service JWT token generation
- [ ] Configure HttpClient with base URL (http://storage:7990)
- [ ] Register in DI container

**Files to create**:
- `mediconyx-api/Services/IStorageServiceClient.cs`
- `mediconyx-api/Services/StorageServiceClient.cs`

---

### [ ] TASK-019: Update File Upload to Use Storage Service
**Priority**: High  
**Effort**: 2h  
**Dependencies**: TASK-018

- [ ] Modify POST /api/sales/requests/{id}/attachments to call storage service
- [ ] Update SalesRequestAttachment to store storage service fileId
- [ ] Add DocumentType parameter to upload endpoint
- [ ] Remove local file saving logic
- [ ] Update file download endpoint to proxy from storage service

**Files to modify**:
- `mediconyx-api/Controllers/SalesRequestsController.cs`

---

### [ ] TASK-020: Create Tenant Provisioning Service
**Priority**: High  
**Effort**: 4h  
**Dependencies**: TASK-005, TASK-007

- [ ] Create ITenantProvisioningService interface
- [ ] Implement ProvisionTenantAsync method:
  - Create Tenant record
  - Set subscription plan and expiry
  - Configure enabled modules
  - Generate subdomain/identifier
- [ ] Implement CreateAdminUserAsync method:
  - Create User with Admin role
  - Generate secure temporary password
  - Associate with tenant
- [ ] Add transaction handling for atomicity
- [ ] Add rollback logic on failure

**Files to create**:
- `mediconyx-api/Services/ITenantProvisioningService.cs`
- `mediconyx-api/Services/TenantProvisioningService.cs`

---

### [ ] TASK-021: Create Provisioning Controller Endpoint
**Priority**: High  
**Effort**: 2h  
**Dependencies**: TASK-020

- [ ] POST /api/sales/requests/{id}/provision - trigger provisioning
- [ ] Validate payment confirmed before provisioning
- [ ] Call TenantProvisioningService
- [ ] Update SalesRequest status to Provisioning → Active
- [ ] Set ActivatedAt timestamp
- [ ] Create timeline events
- [ ] Send welcome email with credentials
- [ ] Require Admin or Master role

**Files to create**:
- `mediconyx-api/Controllers/ProvisioningController.cs`

---

### [ ] TASK-022: Enhance Email Service for Onboarding
**Priority**: Medium  
**Effort**: 3h  
**Dependencies**: TASK-021

- [ ] Add SendWelcomeEmailAsync method with credentials
- [ ] Add SendProfileApprovalEmailAsync
- [ ] Add SendProfileRejectionEmailAsync with reason
- [ ] Add SendPaymentConfirmationEmailAsync
- [ ] Create email templates (HTML)
- [ ] Add email tracking in timeline events

**Files to modify**:
- `mediconyx-api/Services/IEmailService.cs`
- `mediconyx-api/Services/EmailService.cs`

---

### [ ] TASK-023: Add Approval Workflow Endpoints
**Priority**: High  
**Effort**: 2h  
**Dependencies**: TASK-014, TASK-015

- [ ] POST /api/sales/requests/{id}/approve-profile - admin approves
- [ ] POST /api/sales/requests/{id}/reject-profile - admin rejects with reason
- [ ] Validate all required fields completed
- [ ] Validate all mandatory documents uploaded
- [ ] Update status to AwaitingPayment on approval
- [ ] Send notification emails

**Files to create**:
- `mediconyx-api/Controllers/ApprovalController.cs`

---

## Phase 4: Frontend UI Components

### [ ] TASK-024: Create Onboarding Status Badge Component
**Priority**: Medium  
**Effort**: 1h  
**Dependencies**: None

- [ ] Create StatusBadge component with color mapping
- [ ] Add status-to-color logic (Pending=gray, Qualified=blue, InProgress=yellow, etc.)
- [ ] Export from sales components index

**Files to create**:
- `mediconyx-ui/components/sales/StatusBadge.tsx`

**Files to modify**:
- `mediconyx-ui/components/sales/index.ts`

---

### [ ] TASK-025: Create HospitalProfileForm Component
**Priority**: High  
**Effort**: 4h  
**Dependencies**: None

- [ ] Create form with all hospital profile fields
- [ ] Add client-side validation (required, format, length)
- [ ] Implement form state management with React Hook Form or useState
- [ ] Add address fields with proper layout
- [ ] Add primary/secondary contact sections
- [ ] Create submit handler to call API

**Files to create**:
- `mediconyx-ui/components/sales/HospitalProfileForm.tsx`

---

### [ ] TASK-026: Create TechnicalRequirementsForm Component
**Priority**: High  
**Effort**: 3h  
**Dependencies**: None

- [ ] Create form with technical requirement fields
- [ ] Add number inputs with validation (min/max)
- [ ] Add departments multi-line textarea
- [ ] Add data migration toggle
- [ ] Add IT contact section
- [ ] Create submit handler

**Files to create**:
- `mediconyx-ui/components/sales/TechnicalRequirementsForm.tsx`

---

### [ ] TASK-027: Create ModuleSelectionForm Component
**Priority**: Medium  
**Effort**: 2h  
**Dependencies**: None

- [ ] Create checkbox grid for all modules
- [ ] Add module descriptions
- [ ] Group modules by category (Clinical, Financial, Administrative)
- [ ] Add select all / deselect all functionality
- [ ] Create submit handler

**Files to create**:
- `mediconyx-ui/components/sales/ModuleSelectionForm.tsx`

---

### [ ] TASK-028: Create DocumentUploader Component
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-012

- [ ] Create file upload component with drag-and-drop
- [ ] Add DocumentType selector dropdown
- [ ] Show upload progress bar
- [ ] Display uploaded files list with download/delete actions
- [ ] Add file type and size validation
- [ ] Call storage service via API proxy

**Files to create**:
- `mediconyx-ui/components/sales/DocumentUploader.tsx`

---

### [ ] TASK-029: Create OnboardingTimeline Component
**Priority**: Medium  
**Effort**: 2h  
**Dependencies**: None

- [ ] Create vertical timeline component
- [ ] Display events with icon, description, timestamp
- [ ] Add color coding by event type
- [ ] Show actor name for user actions
- [ ] Auto-refresh or real-time updates

**Files to create**:
- `mediconyx-ui/components/sales/OnboardingTimeline.tsx`

---

### [ ] TASK-030: Create OnboardingKanbanBoard Component
**Priority**: High  
**Effort**: 5h  
**Dependencies**: TASK-024

- [ ] Create kanban columns for each status
- [ ] Implement drag-and-drop with react-beautiful-dnd or dnd-kit
- [ ] Create request cards with hospital name, owner, age, price
- [ ] Add color coding by age
- [ ] Implement status change confirmation dialog
- [ ] Add filters (owner, region, plan)
- [ ] Add search functionality

**Files to create**:
- `mediconyx-ui/components/sales/OnboardingKanbanBoard.tsx`

---

### [ ] TASK-031: Create RequestDetailModal Component
**Priority**: High  
**Effort**: 6h  
**Dependencies**: TASK-025, TASK-026, TASK-027, TASK-028, TASK-029

- [ ] Create modal with tabs: Overview, Profile, Technical, Modules, Documents, Pricing, Payment, Notes, Timeline
- [ ] Implement tab navigation
- [ ] Load data for each tab lazily
- [ ] Add edit mode for Sales-owned fields
- [ ] Add quick actions toolbar (Claim, Approve, Reject, Provision)
- [ ] Add close/back navigation

**Files to create**:
- `mediconyx-ui/components/sales/RequestDetailModal.tsx`

---

### [ ] TASK-032: Update Sales Dashboard Page
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-030, TASK-031

- [ ] Replace PipelineTable with OnboardingKanbanBoard
- [ ] Add "Add Lead" button for manual entry
- [ ] Integrate RequestDetailModal trigger
- [ ] Update API calls to fetch new status fields
- [ ] Add loading states and error handling

**Files to modify**:
- `mediconyx-ui/app/portal/sales/page.tsx`

---

### [ ] TASK-033: Create Admin Review Queue Component
**Priority**: High  
**Effort**: 4h  
**Dependencies**: TASK-024

- [ ] Create table with InProgress requests
- [ ] Add columns: hospital, submission date, owner, documents count
- [ ] Add "Review" button to open detail modal
- [ ] Add "Approve" and "Reject" quick actions
- [ ] Add rejection reason modal
- [ ] Refresh list after actions

**Files to create**:
- `mediconyx-ui/components/admin/ReviewQueue.tsx`

---

### [ ] TASK-034: Create Admin Provisioning Queue Component
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-024

- [ ] Create table with AwaitingPayment requests (payment confirmed)
- [ ] Add columns: hospital, payment date, plan, user count
- [ ] Add "Provision Tenant" button
- [ ] Show provisioning progress/status
- [ ] Add confirmation dialog for provisioning

**Files to create**:
- `mediconyx-ui/components/admin/ProvisioningQueue.tsx`

---

### [ ] TASK-035: Update Admin Dashboard Page
**Priority**: High  
**Effort**: 2h  
**Dependencies**: TASK-033, TASK-034

- [ ] Add ReviewQueue component
- [ ] Add ProvisioningQueue component
- [ ] Add section headers and counts
- [ ] Update layout for new sections

**Files to modify**:
- `mediconyx-ui/app/portal/admin/page.tsx`

---

### [ ] TASK-036: Add API Client Methods
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-014 to TASK-023

- [ ] Add hospitalProfile.create, hospitalProfile.get
- [ ] Add technicalRequirements.create, technicalRequirements.get
- [ ] Add moduleSelection.update, moduleSelection.get
- [ ] Add salesRequests.qualify, reject, suspend, reactivate
- [ ] Add approveProfile, rejectProfile
- [ ] Add provisionTenant
- [ ] Add TypeScript interfaces for all DTOs

**Files to modify**:
- `mediconyx-ui/lib/apiClient.ts`

---

## Phase 5: Integration Testing & Refinement

### [ ] TASK-037: End-to-End Test - Customer Initiated Flow
**Priority**: High  
**Effort**: 2h  
**Dependencies**: All previous tasks

- [ ] Submit public onboarding request via form
- [ ] Verify sales notification email sent
- [ ] Login as Sales user, claim lead
- [ ] Update status to Qualified
- [ ] Submit hospital profile, technical requirements, modules
- [ ] Upload required documents
- [ ] Login as Admin, approve profile
- [ ] Configure pricing as Sales
- [ ] Confirm payment manually
- [ ] Login as Admin, provision tenant
- [ ] Verify welcome email sent
- [ ] Login as hospital admin user (new credentials)
- [ ] Verify tenant isolation

**Test Script**: Document steps and results

---

### [ ] TASK-038: End-to-End Test - Sales Initiated Flow
**Priority**: High  
**Effort**: 1.5h  
**Dependencies**: All previous tasks

- [ ] Login as Sales, create lead manually
- [ ] Enter hospital information on behalf of customer
- [ ] Upload documents
- [ ] Qualify and proceed through workflow
- [ ] Verify all stages work as expected

**Test Script**: Document steps and results

---

### [ ] TASK-039: Performance Testing
**Priority**: Medium  
**Effort**: 2h  
**Dependencies**: TASK-037, TASK-038

- [ ] Test public form submission time (target < 2s)
- [ ] Test sales dashboard load time (target < 1s)
- [ ] Test request detail load time (target < 500ms)
- [ ] Test document upload (2MB file, target < 5s)
- [ ] Test status update persistence (target < 300ms)
- [ ] Document results and optimize bottlenecks

---

### [ ] TASK-040: Security Audit
**Priority**: High  
**Effort**: 3h  
**Dependencies**: All previous tasks

- [ ] Verify role-based access control on all endpoints
- [ ] Test unauthorized access attempts
- [ ] Verify file upload validation (malicious files)
- [ ] Check SQL injection vulnerabilities
- [ ] Verify XSS protection
- [ ] Test CSRF protection
- [ ] Verify sensitive data masking in UI
- [ ] Check audit logging completeness

---

### [ ] TASK-041: UI Polish and Responsive Design
**Priority**: Medium  
**Effort**: 4h  
**Dependencies**: TASK-032, TASK-035

- [ ] Test all components on mobile, tablet, desktop
- [ ] Fix responsive layout issues
- [ ] Add loading spinners and skeletons
- [ ] Add error toast notifications
- [ ] Add success confirmations
- [ ] Improve form validation error messages
- [ ] Add keyboard navigation support
- [ ] Test accessibility (screen readers)

---

### [ ] TASK-042: Documentation and Deployment
**Priority**: Medium  
**Effort**: 3h  
**Dependencies**: All previous tasks

- [ ] Update API Swagger documentation
- [ ] Create user guide for Sales role
- [ ] Create user guide for Admin role
- [ ] Update README with new services
- [ ] Document docker-compose changes
- [ ] Create database backup procedure
- [ ] Test deployment on staging environment
- [ ] Prepare production deployment checklist

---

## Phase 6: Post-Launch Monitoring

### [ ] TASK-043: Add Monitoring and Alerts
**Priority**: Medium  
**Effort**: 2h  
**Dependencies**: TASK-042

- [ ] Add health check endpoints for API and storage service
- [ ] Configure log aggregation
- [ ] Set up error rate alerts
- [ ] Set up performance degradation alerts
- [ ] Monitor storage service availability
- [ ] Track onboarding funnel metrics

---

### [ ] TASK-044: Bug Fixes and User Feedback
**Priority**: Ongoing  
**Effort**: Variable  
**Dependencies**: TASK-042

- [ ] Collect user feedback from Sales and Admin teams
- [ ] Triage and prioritize bug reports
- [ ] Implement fixes
- [ ] Deploy hotfixes as needed

---

## Summary

**Total Tasks**: 44  
**Estimated Total Effort**: ~95 hours (~12 working days for single developer)

**Critical Path**:
1. Database schema (TASK-001 to TASK-007): 8 hours
2. Storage service (TASK-008 to TASK-012): 11 hours
3. Backend APIs (TASK-013 to TASK-023): 27 hours
4. Frontend UI (TASK-024 to TASK-036): 42 hours
5. Testing & Launch (TASK-037 to TASK-042): 17 hours

**Phases Overview**:
- Phase 1: Database Schema - 8 hours
- Phase 2: Storage Service - 11 hours
- Phase 3: Backend APIs - 27 hours
- Phase 4: Frontend UI - 42 hours
- Phase 5: Testing - 12 hours
- Phase 6: Monitoring - 2+ hours
