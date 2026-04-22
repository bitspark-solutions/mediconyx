namespace mediconyx_api.Models;

public class SalesRequest
{
    public int Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Notes { get; set; }
    public SalesRequestStatus Status { get; set; } = SalesRequestStatus.Pending;
    public int? OwnerSalesUserId { get; set; }
    public User? OwnerSalesUser { get; set; }
    public string? SourceIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? SubscriptionPlan { get; set; }
    public BillingCycle? BillingCycle { get; set; }
    public string? Currency { get; set; }
    public decimal? QuotedPrice { get; set; }
    public int? Seats { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    
    // Extended subscription fields
    public DateTime? ContractStartDate { get; set; }
    public int? ContractDuration { get; set; } // in months
    public bool AutoRenewal { get; set; } = false;
    public int? UserLicenses { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentTerms? PaymentTerms { get; set; }
    public decimal? DiscountApplied { get; set; } // percentage 0-100
    public string? SpecialTerms { get; set; }
    
    // Tenant link
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public HospitalProfile? HospitalProfile { get; set; }
    public TechnicalRequirements? TechnicalRequirements { get; set; }
    public ModuleSelection? ModuleSelection { get; set; }
    public ICollection<SalesRequestAttachment> Attachments { get; set; } = new List<SalesRequestAttachment>();
    public ICollection<SalesRequestNote> NotesList { get; set; } = new List<SalesRequestNote>();
    public ICollection<OnboardingTimelineEvent> Timeline { get; set; } = new List<OnboardingTimelineEvent>();
}


