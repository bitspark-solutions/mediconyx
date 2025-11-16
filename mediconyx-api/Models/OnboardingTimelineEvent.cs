namespace mediconyx_api.Models;

public class OnboardingTimelineEvent
{
    public int Id { get; set; }
    public int SalesRequestId { get; set; }
    public SalesRequest SalesRequest { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime HappenedAt { get; set; } = DateTime.UtcNow;
}
