using Microsoft.Extensions.Options;

namespace mediconyx_api.Services.AI;

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
