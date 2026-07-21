using Microsoft.EntityFrameworkCore;
using mediconyx_api.Models;

namespace mediconyx_api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<SalesRequest> SalesRequests { get; set; }
    public DbSet<SalesRequestAttachment> SalesRequestAttachments { get; set; }
    public DbSet<SalesRequestNote> SalesRequestNotes { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<OnboardingTimelineEvent> OnboardingTimelineEvents { get; set; }

    // New entities for hospital onboarding
    public DbSet<HospitalProfile> HospitalProfiles { get; set; }
    public DbSet<TechnicalRequirements> TechnicalRequirements { get; set; }
    public DbSet<ModuleSelection> ModuleSelections { get; set; }

    // Core hospital module entities
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    public DbSet<MedicalNote> MedicalNotes { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<VitalSign> VitalSigns { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<HospitalService> HospitalServices { get; set; }

    // AI RAG entities
    public DbSet<FileAttachment> FileAttachments { get; set; }
    public DbSet<AIConversation> AIConversations { get; set; }
    public DbSet<AIMessage> AIMessages { get; set; }

    // Hospital management entities
    public DbSet<DoctorProfile> DoctorProfiles { get; set; }

    // RBAC (Tier 1.1)
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // Multi-role (Tier 1.2)
    public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }

    // IPD / Inpatient (Tier 2.1)
    public DbSet<Ward> Wards { get; set; }
    public DbSet<Bed> Beds { get; set; }
    public DbSet<Admission> Admissions { get; set; }
    public DbSet<AdmissionTransfer> AdmissionTransfers { get; set; }
    public DbSet<DischargeSummary> DischargeSummaries { get; set; }

    // Multi-branch (Tier 4.1)
    public DbSet<Branch> Branches { get; set; }

    // Per-bed-day billing (Tier 3.1)
    public DbSet<BedCharge> BedCharges { get; set; }

    // Telemedicine (Tier 3.3)
    public DbSet<TelehealthSession> TelehealthSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
