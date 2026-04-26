namespace mediconyx_api.Services.AI;

public class LLMResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public float Confidence { get; set; }
}
