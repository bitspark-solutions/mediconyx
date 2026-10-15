using System.Text.Json;

namespace mediconyx_api.Models;

public class AIMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }

    public AIMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    // Citations stored as JSON array
    public string? CitationsJson { get; set; }

    // Confidence score from LLM (0.0 - 1.0)
    public float? Confidence { get; set; }

    // Tokens used for this message
    public int? TokensUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
