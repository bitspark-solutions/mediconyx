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
    }
}

