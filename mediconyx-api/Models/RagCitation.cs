using System.Text.Json;

namespace mediconyx_api.Models;

// DTO for citations
public class RagCitation
{
    public string SourceType { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
}
