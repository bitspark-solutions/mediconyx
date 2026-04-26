namespace mediconyx_api.Services.AI;

public interface ILLMProviderFactory
{
    ILLMService GetLLMService();
    IEmbeddingService GetEmbeddingService();
    string GetCurrentProvider();
    string GetCurrentEmbeddingProvider();
}
