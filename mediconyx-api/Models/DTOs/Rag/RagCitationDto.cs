namespace mediconyx_api.Models.DTOs;

public class RagCitationDto
{
    public string SourceType { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
}
