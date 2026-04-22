using Microsoft.Extensions.Options;
using mediconyx_api.Services.Interfaces;

namespace mediconyx_api.Services;

/// <summary>
/// Configuration for selecting LLM provider
/// </summary>
public class LLMProviderOptions
{
    public string Provider { get; set; } = "OpenAI"; // OpenAI, Ollama, Alibaba, AWS, Azure, etc.
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o";
}

public interface ILLMProviderFactory
{
    ILLMService GetLLMService();
    IEmbeddingService GetEmbeddingService();
    string GetCurrentProvider();
    string GetCurrentEmbeddingProvider();
}

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly ILLMService _llmService;
    private readonly IEmbeddingService _embeddingService;
    private readonly LLMProviderOptions _llmOptions;
    private readonly GenericEmbeddingOptions _embeddingOptions;

    public LLMProviderFactory(
        ILLMService llmService,
        IEmbeddingService embeddingService,
        IOptions<LLMProviderOptions> llmOptions,
        IOptions<GenericEmbeddingOptions> embeddingOptions)
    {
        _llmService = llmService;
        _embeddingService = embeddingService;
        _llmOptions = llmOptions.Value;
        _embeddingOptions = embeddingOptions.Value;
    }

    public ILLMService GetLLMService() => _llmService;
    public IEmbeddingService GetEmbeddingService() => _embeddingService;
    public string GetCurrentProvider() => _llmOptions.Provider;
    public string GetCurrentEmbeddingProvider() => _embeddingOptions.Provider;
}
