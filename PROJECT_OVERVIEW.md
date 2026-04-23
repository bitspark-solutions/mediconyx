# Mediconyx - Hospital Management System with AI

## Project Overview

Mediconyx is a **multi-tenant SaaS hospital management system** with built-in AI capabilities. It enables hospitals to manage their operations digitally while leveraging AI for clinical decision support and patient self-service.

---

## The Story

### Hospital Onboarding Flow

1. **Sales Pipeline**: Hospitals discover Medicinyx and submit a sales request through the public website
2. **Qualification**: Sales team qualifies the lead, collects requirements
3. **Hospital Profile Setup**: Hospital uploads logo, business certificates, government documents, sets address and contact info
4. **Service Configuration**: Hospital defines services they offer (Consultation, Therapy, Surgery, Diagnosis, Lab, Radiology, etc.)
5. **Department Setup**: Hospital creates departments (Cardiology, Neurology, Orthopedics, Medicine, etc.)
6. **Doctor Onboarding**: Doctors are added with their specialization, medical license, department assignment, and schedule
7. **Tenant Activation**: Hospital is live and ready to serve patients

### Daily Operations Flow

```
Patient arrives at hospital
        │
        ▼
Receptionist registers/looks up patient
        │
        ▼
Patient selects service (Consultation, Lab, Radiology, etc.)
        │
        ▼
Appointment created with doctor
        │
        ▼
Doctor sees patient → AI loads patient history (RAG)
        │
        ▼
Doctor reviews: past notes, prescriptions, vitals, uploaded reports
        │
        ▼
Doctor creates: medical notes, prescriptions, diagnoses
        │
        ▼
System stores everything → AI learns from it
        │
        ▼
Patient can log in → View diagnosis, chat with AI about medications
```

### AI Integration Story

**Doctor Side (AI Assist)**:
- When doctor opens a patient, AI instantly summarizes all history
- "Mr. Brown has been seen 5 times. Last visit: Feb 2026. Current medications: Amlodipine 5mg. Last BP: 140/90..."
- AI suggests possible conditions based on history
- AI warns about drug interactions

**Patient Side (AI Assistant)**:
- Patient logs in and asks: "How should I take my heart medication?"
- AI responds based on doctor's prescription notes
- Patient asks: "What did the doctor say about my condition?"
- AI explains in plain language
- Patient can upload reports (X-rays, blood tests) and AI helps organize them

---

## User Roles

| Role | Description |
|------|-------------|
| **Master** | Super admin - owns the platform |
| **Admin** | Hospital admin - manages their hospital |
| **Doctor** | Provides medical services |
| **Nurse** | Assists doctors |
| **Receptionist** | Manages patient registration and appointments |
| **Patient** | Uses patient portal, AI chat, views their records |
| **Sales** | Manages sales pipeline, hospital onboarding |

---

## Feature Modules

### 1. Sales & Onboarding
- Public sales request form with captcha
- Sales pipeline dashboard (Pending → Qualified → In Progress → Awaiting Payment → Provisioning → Active)
- Hospital profile collection (legal name, license, tax ID, address, contact)
- Technical requirements collection
- Module selection (OPD, IPD, Emergency, Pharmacy, Lab, Radiology, Billing, etc.)
- Approval workflow
- Tenant provisioning (creates tenant, admin user, enables modules)

### 2. Hospital Settings
- Upload hospital logo
- Hospital name, address, contact info
- Operating hours (JSON: {"monday":"9am-5pm",...})
- Business registration number
- Module toggles (enable/disable features)
- Subscription info (plan, licenses, expiry)

### 3. Services Management
- CRUD for hospital services
- Service types: Consultation, Therapy, Surgery, Diagnosis, Lab, Radiology, Pharmacy, Emergency
- Each service has: name, type, description, duration (minutes), price (optional), linked department

### 4. Department Management
- CRUD for departments
- Department code, name, description
- Link to services
- Link to doctors (head of department)

### 5. Doctor Management
- Create doctor: user account + doctor profile
- Medical license number
- Specialization (General, Cardiology, Orthopedics, Neurology, Pediatrics, etc.)
- Department assignment
- Consultation duration (15/30/45/60 min)
- Consultation fee
- Accepting patients toggle
- Active/Inactive status

### 6. Doctor Schedule Management
- Weekly schedule per doctor
- Set available time slots per day
- Block time off (vacation, meetings)
- Recurring availability patterns

### 7. Patient Management
- Staff-initiated registration (Receptionist creates patient)
- Patient self-registration (via public form)
- Patient fields: Name, DOB, Gender, Blood Group, Phone, Email, National ID, Address, Emergency Contact
- Auto-generated patient number (P-000001)
- Search by name, phone, email, national ID

### 8. Appointment Management
- Create appointment: Patient + Doctor + Service + DateTime
- Appointment types: Consultation, Follow-up, Emergency
- Status: Scheduled, Confirmed, In Progress, Completed, Cancelled
- Notes field
- Cancel with reason

### 9. Medical Records
- **Medical Notes**: Clinical notes by doctor, linked to appointment, note types (SOAP, Progress, Discharge, etc.)
- **Prescriptions**: Medications prescribed, instructions, allergies noted
- **Vital Signs**: BP, HR, Temperature, Weight, Height, SpO2, Blood Glucose
- **File Attachments**: Uploaded reports (Lab reports, X-rays, MRI, CT scans, prescriptions, discharge summaries)

### 10. AI Integration (RAG)

#### Backend Components
- `GenericLlmService.cs` - Works with any LLM provider (OpenAI, Ollama, Alibaba, AWS, Azure)
- `GenericEmbeddingService.cs` - Works with any embedding provider
- `RagQueryService.cs` - Retrieves patient context, builds prompt, returns AI response with citations
- `RagController.cs` - API endpoints for chat and summary

#### AI Features
- **Patient Summary for Doctors**: AI summarizes all patient history (notes, prescriptions, vitals, files)
- **AI Chat for Patients**: Patient asks questions about their health, AI responds based on their records
- **Citations**: Every AI response cites the source documents
- **Suggested Actions**: AI suggests next steps based on context

#### AI Configuration (appsettings)
```json
"LLM": {
  "Provider": "OpenAI",  // or Ollama, Alibaba, AWS, Azure
  "BaseUrl": "https://api.openai.com/v1",
  "Model": "gpt-4o",
  "ApiKey": "..."
},
"Embedding": {
  "Provider": "OpenAI",
  "BaseUrl": "https://api.openai.com/v1",
  "Model": "text-embedding-3-small",
  "ApiKey": "..."
}
```

### 11. Patient Portal
- AI Assistant (chat with AI about health)
- My Reports (upload/view files)
- Appointments (view upcoming/past)
- My Profile (view/edit info)

### 12. Doctor Portal
- Patient list with search
- Patient detail view with AI summary
- AI-assisted consultation
- Today's appointments

---

## Database Schema

### Core Entities

```
Tenant (Hospital)
├── Users (Admin, Doctor, Nurse, Receptionist, Patient)
├── Departments
├── Doctors (DoctorProfile linked to User)
├── Patients
│   ├── Appointments
│   ├── MedicalNotes
│   ├── Prescriptions
│   ├── VitalSigns
│   └── FileAttachments
├── HospitalServices
└── DoctorSchedules

SalesRequest (for onboarding)
├── HospitalProfile
├── TechnicalRequirements
├── ModuleSelection
└── OnboardingTimelineEvent
```

### Key Tables

| Table | Purpose |
|-------|---------|
| `Tenants` | Hospital accounts |
| `Users` | All user accounts (includes Doctors, Patients) |
| `Patients` | Patient records |
| `Departments` | Hospital departments |
| `HospitalServices` | Services offered |
| `DoctorProfiles` | Doctor specialization (links to User) |
| `DoctorSchedules` | Doctor availability |
| `Appointments` | Patient appointments |
| `MedicalNotes` | Clinical notes |
| `Prescriptions` | Medication prescriptions |
| `VitalSigns` | Vitals recordings |
| `FileAttachments` | Uploaded files |
| `AIConversations` | Patient AI chat sessions |
| `AIMessages` | Chat messages |

---

## API Endpoints

### Tenant Settings
- `GET /api/tenant-settings` - Get tenant settings
- `PUT /api/tenant-settings` - Update settings
- `POST /api/tenant-settings/logo` - Upload logo

### Services
- `GET /api/service` - List services
- `POST /api/service` - Create service
- `PUT /api/service/{id}` - Update service
- `DELETE /api/service/{id}` - Soft delete service

### Doctors
- `GET /api/doctor` - List doctors
- `GET /api/doctor/{id}` - Get doctor
- `POST /api/doctor` - Create doctor (user + profile)
- `PUT /api/doctor/{id}` - Update doctor
- `DELETE /api/doctor/{id}` - Deactivate doctor
- `GET /api/doctor/by-specialization/{specialization}` - List by specialization

### Doctor Schedule
- `GET /api/doctor-schedule` - Get schedules
- `POST /api/doctor-schedule` - Create schedule
- `PUT /api/doctor-schedule/{id}` - Update
- `DELETE /api/doctor-schedule/{id}` - Delete

### Patients
- `GET /api/patient` - List patients
- `GET /api/patient/{id}` - Get patient
- `POST /api/patient` - Create patient
- `PUT /api/patient/{id}` - Update patient

### Appointments
- `GET /api/appointment` - List appointments
- `POST /api/appointment` - Create appointment
- `PUT /api/appointment/{id}` - Update
- `PATCH /api/appointment/{id}/cancel` - Cancel

### Medical Records
- `GET /api/medical-note/patient/{patientId}` - Get notes
- `POST /api/medical-note` - Create note
- `GET /api/prescription/patient/{patientId}` - Get prescriptions
- `POST /api/prescription` - Create prescription
- `GET /api/vital-sign/patient/{patientId}` - Get vitals
- `POST /api/vital-sign` - Record vitals

### File Attachments
- `POST /api/file-attachment/upload` - Upload file
- `GET /api/file-attachment/{id}` - Get metadata
- `GET /api/file-attachment/{id}/download` - Download
- `GET /api/file-attachment/patient/{patientId}` - List patient's files
- `DELETE /api/file-attachment/{id}` - Soft delete

### AI (RAG)
- `POST /api/rag/patient/{patientId}/chat` - Chat with AI
- `GET /api/rag/patient/{patientId}/summary` - Get AI summary
- `GET /api/rag/patient/{patientId}/conversations` - List conversations
- `GET /api/rag/conversation/{id}/messages` - Get messages

---

## Frontend Pages

### Public
- `/` - Landing page with sales form

### Admin Portal (`/portal/admin`)
- Dashboard
- Hospital Settings (logo, info, modules)
- Service Management
- Department Management
- Doctor List & Registration
- Patient List

### Doctor Portal (`/portal/doctor`)
- Patient List (with AI summary panel)
- Patient Detail (notes, prescriptions, vitals, files)
- Today's Appointments
- AI Assist

### Patient Portal (`/portal/patient`)
- AI Assistant (chat)
- My Reports (upload/view)
- Appointments
- My Profile

### Sales Portal (`/portal/sales`)
- Pipeline dashboard
- Request detail/management

---

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8
- **Database**: PostgreSQL 18 with pgvector (for vector search)
- **ORM**: Entity Framework Core
- **Auth**: JWT with TenantId claims
- **Storage**: MinIO (S3-compatible) for files

### Frontend
- **Framework**: Next.js 14 (React)
- **Styling**: TailwindCSS
- **State**: Zustand
- **Icons**: Lucide React

### AI Services
- Any OpenAI-compatible REST API
- Tested with: OpenAI, Ollama (local), Alibaba DashScope

---

## Important Notes

### Multi-Tenant Isolation
- Every API call filtered by `TenantId` from JWT
- Patients can't see other patients' data
- Doctors can only see their tenant's data

### AI Safety
- AI responses include citations (source documents)
- Patient-facing AI has disclaimer: "Consult your doctor for medical advice"
- AI suggestions for doctors marked as "not a definitive diagnosis"

### File Upload
- Max size: 50MB
- Allowed types: PDF, JPG, JPEG, PNG, GIF
- Categories: LabReport, Imaging, Prescription, DischargeSummary, Other

---

## Future Enhancements (Not Yet Implemented)

- pgvector integration for semantic search
- Multi-modal AI for report analysis (X-rays, CT scans)
- SMS/Email notifications
- Payment/Billing module
- Inventory management
- Telemedicine integration
- Analytics/Reporting dashboard
- Patient self-registration flow
- Doctor profile page
- Appointment rescheduling

---

## Quick Reference

### Starting the App
```bash
# Backend (mediconyx-api)
dotnet run

# Frontend (mediconyx-ui)
npm run dev
```

### Test Accounts
- Master: master@mediconyx.local / MasterPass123!
- Sales: sales@mediconyx.local / Sales@123
- Admin: admin@mediconyx.local / Admin@123

### API Base URL
- Local: http://localhost:9765

### Swagger Docs
- http://localhost:9765/swagger