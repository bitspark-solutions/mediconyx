namespace mediconyx_api.Models.DTOs;

/// <summary>Master-only subscription update for a tenant (plan, seats, validity window).</summary>
public class UpdateSubscriptionRequest
{
    public string? SubscriptionPlan { get; set; }
    public int? UserLicenses { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}
