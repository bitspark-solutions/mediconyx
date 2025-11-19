namespace mediconyx_api.Models.DTOs;

using mediconyx_api.Models;

public class PublicSalesRequestCreate
{
    public string HospitalName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Notes { get; set; }
    public string CaptchaToken { get; set; } = string.Empty;
    public string? SubscriptionPlan { get; set; }
    public BillingCycle? BillingCycle { get; set; }
    public string? Currency { get; set; }
    public decimal? QuotedPrice { get; set; }
    public int? Seats { get; set; }
}

public class SalesRequestQuery
{
    public SalesRequestStatus? Status { get; set; }
    public int? OwnerId { get; set; }
    public string? Region { get; set; }
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SalesStatusUpdate
{
    public SalesRequestStatus Status { get; set; }
    public string? Reason { get; set; }
}

public class PricingUpdate
{
    public string? SubscriptionPlan { get; set; }
    public BillingCycle? BillingCycle { get; set; }
    public string? Currency { get; set; }
    public decimal? QuotedPrice { get; set; }
    public int? Seats { get; set; }
}

public class PaymentStatusUpdate
{
    public PaymentStatus PaymentStatus { get; set; }
}

public class StatusActionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class RejectProfileRequest
{
    public string Reason { get; set; } = string.Empty;
    public List<string> RequiredChanges { get; set; } = new();
}
