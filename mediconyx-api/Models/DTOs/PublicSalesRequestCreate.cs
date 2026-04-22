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
