namespace mediconyx_api.Models.DTOs;

using mediconyx_api.Models;

public class PricingUpdate
{
    public string? SubscriptionPlan { get; set; }
    public BillingCycle? BillingCycle { get; set; }
    public string? Currency { get; set; }
    public decimal? QuotedPrice { get; set; }
    public int? Seats { get; set; }
}
