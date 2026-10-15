namespace mediconyx_api.Models.DTOs;

public class RagQueryResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<RagCitationDto> Citations { get; set; } = new();
    public float Confidence { get; set; }
    public List<string> SuggestedActions { get; set; } = new();
    public int ConversationId { get; set; }
    public int MessageId { get; set; }
}
