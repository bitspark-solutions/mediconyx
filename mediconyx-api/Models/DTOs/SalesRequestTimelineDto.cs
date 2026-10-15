namespace mediconyx_api.Models.DTOs;

public class SalesRequestTimelineDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime HappenedAt { get; set; }
}
