using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace mediconyx_api.Services.AI;

public class GenericLlmService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly GenericLlmOptions _options;
    private readonly ILogger<GenericLlmService> _logger;

    public GenericLlmService(
        HttpClient httpClient,
        IOptions<GenericLlmOptions> options,
        ILogger<GenericLlmService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/'));

        // Set authentication header based on provider
        SetAuthentication();
    }

    private void SetAuthentication()
    {
        switch (_options.AuthType.ToUpperInvariant())
        {
            case "BEARER":
                if (!string.IsNullOrEmpty(_options.ApiKey))
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
                break;

            case "APIKEY":
                if (!string.IsNullOrEmpty(_options.ApiKey))
                    _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
                break;

            case "AZUREAD":
                // Azure AD tokens are acquired separately, here we just set the header format
                if (!string.IsNullOrEmpty(_options.ApiKey))
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
                break;
            // AWS SigV4 and other auth types handled separately
        }
    }

    public async Task<LLMResponse> GenerateChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        CancellationToken ct = default)
    {
        try
        {
            var messages = new List<GenericChatMessage>();

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new GenericChatMessage { Role = "system", Content = systemPrompt });
            }

            if (conversationHistory != null)
            {
                foreach (var msg in conversationHistory)
                {
                    messages.Add(new GenericChatMessage { Role = msg.Role, Content = msg.Content });
                }
            }

            messages.Add(new GenericChatMessage { Role = "user", Content = userMessage });

            var request = BuildRequest(messages);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_options.ChatEndpoint, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("{Provider} API error: {StatusCode} - {Error}",
                    _options.Provider, response.StatusCode, error);
                throw new Exception($"LLM generation failed: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<GenericChatResponse>(ct);

            if (result?.Choices == null || result.Choices.Count == 0)
            {
                throw new Exception("No response from LLM provider");
            }

            return new LLMResponse
            {
                Content = result.Choices[0].Message?.Content ?? string.Empty,
                Model = result.Model ?? _options.Model,
                TokensUsed = result.Usage?.TotalTokens ?? 0,
                Confidence = 0.8f // Generic providers don't always return confidence
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM response from {Provider}", _options.Provider);
            throw;
        }
    }

    private object BuildRequest(List<GenericChatMessage> messages)
    {
        // Build request based on provider type
        switch (_options.Provider.ToUpperInvariant())
        {
            case "OLLAMA":
                return new OllamaFormatRequest
                {
                    Model = _options.Model,
                    Messages = messages,
                    Stream = false,
                    Options = new OllamaOptions
                    {
                        Temperature = _options.Temperature,
                        NumPredict = _options.MaxTokens,
                        NumCtx = _options.NumCtx
                    }
                };

            case "ALIBABA": // Alibaba Cloud (Qwen)
            case "DASHSCOPE":
                return new AlibabaFormatRequest
                {
                    Model = _options.Model,
                    Input = new AlibabaInput { Messages = messages },
                    Parameters = new AlibabaParameters
                    {
                        Temperature = _options.Temperature,
                        MaxTokens = _options.MaxTokens
                    }
                };

            case "AWS":
            case "BEDROCK":
                return new BedrockFormatRequest
                {
                    ModelId = _options.Model,
                    Messages = messages,
                    InferenceConfig = new BedrockInferenceConfig
                    {
                        Temperature = _options.Temperature,
                        MaxTokens = _options.MaxTokens
                    }
                };

            default: // OpenAI compatible
                return new OpenAIFormatRequest
                {
                    Model = _options.Model,
                    Messages = messages,
                    Temperature = _options.Temperature,
                    MaxTokens = _options.MaxTokens
                };
        }
    }

    public string GetProviderName() => _options.Provider;

    // Request/Response DTOs for different providers

    private class GenericChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    // OpenAI Compatible Format
    private class OpenAIFormatRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<GenericChatMessage> Messages { get; set; } = new();
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
    }

    private class OpenAIResponse
    {
        public string Model { get; set; } = string.Empty;
        public List<OpenAIChoice> Choices { get; set; } = new();
        public OpenAIUsage? Usage { get; set; }
    }

    private class OpenAIChoice
    {
        public GenericChatMessage? Message { get; set; }
    }

    private class OpenAIUsage
    {
        public int TotalTokens { get; set; }
    }

    // Ollama Format
    private class OllamaFormatRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<GenericChatMessage> Messages { get; set; } = new();
        public bool Stream { get; set; }
        public OllamaOptions? Options { get; set; }
    }

    private class OllamaOptions
    {
        public float Temperature { get; set; }
        public int NumPredict { get; set; }
        public int? NumCtx { get; set; }
    }

    private class OllamaResponse
    {
        public GenericChatMessage? Message { get; set; }
        public int? EvalCount { get; set; }
    }

    // Alibaba/DashScope Format
    private class AlibabaFormatRequest
    {
        public string Model { get; set; } = string.Empty;
        public AlibabaInput Input { get; set; } = new();
        public AlibabaParameters Parameters { get; set; } = new();
    }

    private class AlibabaInput
    {
        public List<GenericChatMessage> Messages { get; set; } = new();
    }

    private class AlibabaParameters
    {
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
    }

    private class AlibabaResponse
    {
        public string Model { get; set; } = string.Empty;
        public AlibabaOutput? Output { get; set; }
        public AlibabaUsage? Usage { get; set; }
    }

    private class AlibabaOutput
    {
        public GenericChatMessage? Message { get; set; }
    }

    private class AlibabaUsage
    {
        public int TotalTokens { get; set; }
    }

    // AWS Bedrock Format
    private class BedrockFormatRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public List<GenericChatMessage> Messages { get; set; } = new();
        public BedrockInferenceConfig InferenceConfig { get; set; } = new();
    }

    private class BedrockInferenceConfig
    {
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
    }

    private class BedrockResponse
    {
        public string ModelId { get; set; } = string.Empty;
        public BedrockOutput? Output { get; set; }
        public BedrockMetrics? Metrics { get; set; }
    }

    private class BedrockOutput
    {
        public GenericChatMessage? Message { get; set; }
    }

    private class BedrockMetrics
    {
        public int TotalTokens { get; set; }
    }

    // Unified response type for deserialization
    private class GenericChatResponse
    {
        public string? Model { get; set; }
        public List<GenericChoice> Choices { get; set; } = new();
        public GenericUsage? Usage { get; set; }
        public GenericChatMessage? Message { get; set; }
        public int? EvalCount { get; set; }
        public GenericChatMessage? Output { get; set; }
    }

    private class GenericChoice
    {
        public GenericChatMessage? Message { get; set; }
    }

    private class GenericUsage
    {
        public int TotalTokens { get; set; }
    }
}
