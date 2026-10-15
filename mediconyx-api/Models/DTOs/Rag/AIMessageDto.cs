namespace mediconyx_api.Models.DTOs;

public class AIMessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<RagCitationDto>? Citations { get; set; }
    public float? Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
}
