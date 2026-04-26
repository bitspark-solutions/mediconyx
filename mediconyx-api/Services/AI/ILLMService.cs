namespace mediconyx_api.Services.AI;

public interface ILLMService
{
    Task<LLMResponse> GenerateChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        CancellationToken ct = default);

    string GetProviderName();
}
