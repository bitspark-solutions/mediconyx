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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Email });
            entity.HasIndex(e => e.Role); // For filtering by role
            entity.HasIndex(e => e.IsActive); // For filtering active/inactive users
            entity.HasIndex(e => e.TenantId); // For tenant-based queries
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Role)
                .HasConversion<int>();
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Tenant configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
        });

        modelBuilder.Entity<SalesRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            // Composite indexes for performance
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => e.PaidAt).HasFilter("\"PaidAt\" IS NOT NULL");
            entity.HasIndex(e => new { e.OwnerSalesUserId, e.Status });
            entity.HasIndex(e => new { e.Region, e.Status });
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.TenantId).HasFilter("\"TenantId\" IS NOT NULL");
            
            entity.Property(e => e.HospitalName).IsRequired().HasMaxLength(300);
            entity.Property(e => e.ContactPerson).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.SubscriptionPlan).HasMaxLength(100);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.QuotedPrice).HasColumnType("numeric(18,2)");
            entity.Property(e => e.DiscountApplied).HasColumnType("numeric(5,2)");
            entity.Property(e => e.SpecialTerms).HasMaxLength(2000);
            
            entity.HasOne(e => e.OwnerSalesUser)
                .WithMany()
                .HasForeignKey(e => e.OwnerSalesUserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesRequestAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SalesRequestId);
            entity.HasIndex(e => e.Sha256);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sha256).IsRequired().HasMaxLength(64);
            entity.HasOne(e => e.SalesRequest)
                .WithMany(r => r.Attachments)
                .HasForeignKey(e => e.SalesRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesRequestNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SalesRequestId, e.CreatedAt });
            entity.Property(e => e.Note).IsRequired().HasMaxLength(2000);
            entity.HasOne(e => e.SalesRequest)
                .WithMany(r => r.NotesList)
                .HasForeignKey(e => e.SalesRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AuthorUser)
                .WithMany()
                .HasForeignKey(e => e.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityName, e.EntityId, e.CreatedAt });
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<OnboardingTimelineEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SalesRequestId, e.HappenedAt });
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.HasOne(e => e.SalesRequest)
                .WithMany(r => r.Timeline)
                .HasForeignKey(e => e.SalesRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // HospitalProfile configuration
        modelBuilder.Entity<HospitalProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SalesRequestId).IsUnique();
            entity.HasIndex(e => e.LicenseNumber);
            entity.HasIndex(e => e.TaxId);
            
            entity.HasOne(e => e.SalesRequest)
                .WithOne(r => r.HospitalProfile)
                .HasForeignKey<HospitalProfile>(e => e.SalesRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // TechnicalRequirements configuration
        modelBuilder.Entity<TechnicalRequirements>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SalesRequestId).IsUnique();
            
            entity.HasOne(e => e.SalesRequest)
                .WithOne(r => r.TechnicalRequirements)
                .HasForeignKey<TechnicalRequirements>(e => e.SalesRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ModuleSelection configuration
        modelBuilder.Entity<ModuleSelection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SalesRequestId).IsUnique();
            
            entity.HasOne(e => e.SalesRequest)
                .WithOne(r => r.ModuleSelection)
                .HasForeignKey<ModuleSelection>(e => e.SalesRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Patient configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.PatientNumber }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Phone });
            entity.HasIndex(e => new { e.TenantId, e.NationalId })
                .HasFilter("\"NationalId\" IS NOT NULL");
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.NationalId).HasMaxLength(50);
            entity.Property(e => e.PatientNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.BloodGroup).HasMaxLength(5);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(200);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(50);
            entity.Property(e => e.EmergencyContactRelation).HasMaxLength(100);
            entity.Property(e => e.Gender).HasConversion<int>();
        });

        // Department configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Appointment configuration
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.AppointmentDate, e.DoctorId });
            entity.HasIndex(e => new { e.TenantId, e.PatientId });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.CancelReason).HasMaxLength(500);
        });

        // DoctorSchedule configuration
        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.DoctorId, e.DayOfWeek });
            entity.Property(e => e.DayOfWeek).HasConversion<int>();
        });

        // MedicalNote configuration
        modelBuilder.Entity<MedicalNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.PatientId });
            entity.HasIndex(e => new { e.TenantId, e.AuthorUserId });
            entity.HasIndex(e => e.AppointmentId).HasFilter("\"AppointmentId\" IS NOT NULL");
            entity.Property(e => e.NoteType).HasConversion<int>();
            entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
            entity.Property(e => e.Diagnosis).HasMaxLength(500);
        });

        // Prescription configuration
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.PatientId });
            entity.HasIndex(e => new { e.TenantId, e.DoctorId });
            entity.HasIndex(e => e.AppointmentId);
            entity.Property(e => e.Medications).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.Instructions).HasMaxLength(2000);
            entity.Property(e => e.Allergies).HasMaxLength(1000);
        });

        // VitalSign configuration
        modelBuilder.Entity<VitalSign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.PatientId, e.RecordedAt });
            entity.HasIndex(e => e.AppointmentId).HasFilter("\"AppointmentId\" IS NOT NULL");
            entity.Property(e => e.TemperatureCelsius).HasColumnType("numeric(4,1)");
            entity.Property(e => e.WeightKg).HasColumnType("numeric(5,1)");
            entity.Property(e => e.HeightCm).HasColumnType("numeric(5,1)");
            entity.Property(e => e.BloodGlucose).HasColumnType("numeric(5,1)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
        });

        // HospitalService configuration
        modelBuilder.Entity<HospitalService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasIndex(e => new { e.TenantId, e.ServiceType });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ServiceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.BasePrice).HasColumnType("numeric(10,2)");
        });

        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.InvoiceNumber }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.PatientId });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.LineItems).IsRequired().HasMaxLength(10000);
            entity.Property(e => e.SubTotal).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TaxRate).HasColumnType("numeric(5,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.PaidAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.PaymentMethod).HasConversion<int>();
            entity.Property(e => e.PaymentReference).HasMaxLength(200);
        });

        // FileAttachment configuration
        modelBuilder.Entity<FileAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.PatientId });
            entity.HasIndex(e => e.StorageFileId);
            entity.HasIndex(e => new { e.TenantId, e.Category });
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).HasConversion<int>();
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        // AIConversation configuration
        modelBuilder.Entity<AIConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.PatientId });
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
        });

        // AIMessage configuration
        modelBuilder.Entity<AIMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
            entity.Property(e => e.Role).HasConversion<int>();
            entity.Property(e => e.Confidence).HasColumnType("numeric(3,2)");
        });

        // DoctorProfile configuration
        modelBuilder.Entity<DoctorProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasIndex(e => new { e.TenantId, e.Specialization });
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.MedicalLicenseNumber).HasMaxLength(100);
            entity.Property(e => e.Specialization).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ConsultationFee).HasColumnType("numeric(10,2)");
        });

        // Permission configuration
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(300);
        });

        // RolePermission configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Role, e.PermissionId }).IsUnique();
            entity.Property(e => e.Role).HasConversion<int>();
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserRoleAssignment (Tier 1.2)
        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Role }).IsUnique();
            entity.Property(e => e.Role).HasConversion<int>();
            entity.HasOne(e => e.User)
                .WithMany(u => u.AdditionalRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // User has Designation + PrimaryDepartmentId
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Designation).HasMaxLength(150);
            entity.HasOne(e => e.PrimaryDepartment)
                .WithMany()
                .HasForeignKey(e => e.PrimaryDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Department has HeadDoctor
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasOne(e => e.HeadDoctor)
                .WithMany()
                .HasForeignKey(e => e.HeadDoctorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // IPD (Tier 2.1) ──────────────────────────────────────────
        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasIndex(e => new { e.TenantId, e.WardType });
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.WardType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bed>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.WardId, e.Number }).IsUnique();
            entity.Property(e => e.Number).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasOne(e => e.Ward)
                .WithMany(w => w.Beds)
                .HasForeignKey(e => e.WardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Admission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.PatientId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.BedId });
            entity.Property(e => e.AdmissionType).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.DiagnosisOnAdmission).HasMaxLength(2000);
            entity.Property(e => e.DischargeNotes).HasMaxLength(2000);
            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bed)
                .WithMany()
                .HasForeignKey(e => e.BedId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AttendingDoctor)
                .WithMany()
                .HasForeignKey(e => e.AttendingDoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdmissionTransfer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AdmissionId, e.TransferredAt });
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasOne(e => e.Admission)
                .WithMany(a => a.Transfers)
                .HasForeignKey(e => e.AdmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.FromBed)
                .WithMany()
                .HasForeignKey(e => e.FromBedId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ToBed)
                .WithMany()
                .HasForeignKey(e => e.ToBedId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DischargeSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AdmissionId).IsUnique();
            entity.Property(e => e.ConditionAtDischarge).HasConversion<int>();
            entity.Property(e => e.DischargeInstructions).HasMaxLength(2000);
            entity.Property(e => e.MedicationsOnDischarge).HasMaxLength(2000);
            entity.HasOne(e => e.Admission)
                .WithOne(a => a.DischargeSummary)
                .HasForeignKey<DischargeSummary>(e => e.AdmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DischargingDoctor)
                .WithMany()
                .HasForeignKey(e => e.DischargingDoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

