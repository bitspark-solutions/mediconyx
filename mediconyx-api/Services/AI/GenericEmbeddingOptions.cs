namespace mediconyx_api.Services.AI;

public class GenericEmbeddingOptions
{
    public string Provider { get; set; } = "OpenAI";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "text-embedding-3-small";
    public int Dimensions { get; set; } = 1536;
    public string? ApiKey { get; set; }
    public string AuthType { get; set; } = "Bearer";
    public string EmbeddingsEndpoint { get; set; } = "/embeddings";
}
