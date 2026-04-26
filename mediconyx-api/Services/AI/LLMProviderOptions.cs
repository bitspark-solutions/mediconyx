namespace mediconyx_api.Services.AI;

public class LLMProviderOptions
{
    public string Provider { get; set; } = "OpenAI";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o";
}
