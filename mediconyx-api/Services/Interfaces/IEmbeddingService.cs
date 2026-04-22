namespace mediconyx_api.Services.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for a single text
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Generate embedding vectors for multiple texts
    /// </summary>
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken ct = default);

    /// <summary>
    /// Get the provider name (e.g., "OpenAI", "AzureOpenAI")
    /// </summary>
    string GetProviderName();

    /// <summary>
    /// Get the embedding dimensions for this provider
    /// </summary>
    int GetDimensions();
}
