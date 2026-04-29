namespace mediconyx_api.Models;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;

    // Subdomain for tenant isolation
    public string? Subdomain { get; set; }

    // Hospital Branding & Settings
    public string? LogoUrl { get; set; }  // URL to logo in MinIO storage
    public string? Description { get; set; }
    public string? OperatingHours { get; set; }  // JSON: {"monday":"9am-5pm","tuesday":"9am-5pm"...}
    public string? TimeZone { get; set; }
    public string? BusinessRegistrationNumber { get; set; }

    // Subscription/Plan info (for SaaS)
    public string? SubscriptionPlan { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public int? UserLicenses { get; set; }

    // Module configuration (JSON or individual flags)
    public bool ModuleOpdEnabled { get; set; } = false;
    public bool ModuleIpdEnabled { get; set; } = false;
    public bool ModuleEmergencyEnabled { get; set; } = false;
    public bool ModulePharmacyEnabled { get; set; } = false;
    public bool ModuleLaboratoryEnabled { get; set; } = false;
    public bool ModuleRadiologyEnabled { get; set; } = false;
    public bool ModuleBillingEnabled { get; set; } = false;
    public bool ModuleInventoryEnabled { get; set; } = false;
    public bool ModuleHrPayrollEnabled { get; set; } = false;
    public bool ModuleAppointmentEnabled { get; set; } = false;
    public bool ModuleTelemedicineEnabled { get; set; } = false;
    public bool ModuleAnalyticsEnabled { get; set; } = false;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}

