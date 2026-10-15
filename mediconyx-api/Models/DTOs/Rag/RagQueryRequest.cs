namespace mediconyx_api.Models.DTOs;

public class RagQueryRequest
{
    public string Message { get; set; } = string.Empty;
    public int? ConversationId { get; set; }
}
