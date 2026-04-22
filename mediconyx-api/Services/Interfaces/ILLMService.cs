namespace mediconyx_api.Services.Interfaces;

public interface ILLMService
{
    /// <summary>
    /// Generate a chat completion response
    /// </summary>
    Task<LLMResponse> GenerateChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get the provider name
    /// </summary>
    string GetProviderName();
}

public class LLMResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public float Confidence { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;  // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
}
