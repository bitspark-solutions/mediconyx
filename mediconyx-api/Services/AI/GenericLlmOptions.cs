namespace mediconyx_api.Services.AI;

public class GenericLlmOptions
{
    public string Provider { get; set; } = "OpenAI";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.3f;
    public int? NumCtx { get; set; } = 4096;
    public string? ApiKey { get; set; }
    public string AuthType { get; set; } = "Bearer";
    public string? AzureApiVersion { get; set; }
    public string? AwsRegion { get; set; }
    public string? AwsAccessKey { get; set; }
    public string? AwsSecretKey { get; set; }
    public string ChatEndpoint { get; set; } = "/chat/completions";
    public string EmbeddingsEndpoint { get; set; } = "/embeddings";
}
