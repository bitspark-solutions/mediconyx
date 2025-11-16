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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SalesRequestAttachment> Attachments { get; set; } = new List<SalesRequestAttachment>();
    public ICollection<SalesRequestNote> NotesList { get; set; } = new List<SalesRequestNote>();
    public ICollection<OnboardingTimelineEvent> Timeline { get; set; } = new List<OnboardingTimelineEvent>();
}

public enum SalesRequestStatus
{
    Pending = 0,
    InProgress = 1,
    Active = 2,
    Deactivated = 3
}

public enum BillingCycle
{
    Monthly = 0,
    Annual = 1
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}
