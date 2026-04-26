using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace mediconyx_api.Services.AI;

public class GenericEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly GenericEmbeddingOptions _options;
    private readonly ILogger<GenericEmbeddingService> _logger;

    public GenericEmbeddingService(
        HttpClient httpClient,
        IOptions<GenericEmbeddingOptions> options,
        ILogger<GenericEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/'));
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
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var request = BuildRequest(text);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_options.EmbeddingsEndpoint, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("{Provider} embedding error: {StatusCode} - {Error}",
                    _options.Provider, response.StatusCode, error);
                throw new Exception($"Embedding generation failed: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<GenericEmbeddingResponse>(ct);

            if (result?.Data == null || result.Data.Count == 0)
            {
                throw new Exception("No embedding returned from provider");
            }

            return result.Data[0].Embedding.Select(d => (float)d).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding from {Provider}", _options.Provider);
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
    {
        var results = new List<float[]>();

        foreach (var text in texts)
        {
            try
            {
                var embedding = await GenerateEmbeddingAsync(text, ct);
                results.Add(embedding);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for text: {Text}", text[..Math.Min(50, text.Length)]);
                results.Add(new float[_options.Dimensions]);
            }
        }

        return results;
    }

    private object BuildRequest(string text)
    {
        var truncatedText = text.Length > 8000 ? text[..8000] : text;

        switch (_options.Provider.ToUpperInvariant())
        {
            case "OLLAMA":
                return new OllamaEmbedRequest
                {
                    Model = _options.Model,
                    Input = truncatedText
                };

            case "ALIBABA":
            case "DASHSCOPE":
                return new AlibabaEmbedRequest
                {
                    Model = _options.Model,
                    Input = new AlibabaEmbedInput { Text = truncatedText }
                };

            case "AWS":
            case "BEDROCK":
                return new BedrockEmbedRequest
                {
                    ModelId = _options.Model,
                    Content = truncatedText
                };

            default: // OpenAI compatible
                return new OpenAIEmbedRequest
                {
                    Model = _options.Model,
                    Input = truncatedText,
                    Dimensions = _options.Dimensions
                };
        }
    }

    public string GetProviderName() => _options.Provider;
    public int GetDimensions() => _options.Dimensions;

    // Provider-specific request/response types

    private class OpenAIEmbedRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
        public int? Dimensions { get; set; }
    }

    private class OpenAIEmbedResponse
    {
        public List<OpenAIEmbedData>? Data { get; set; }
    }

    private class OpenAIEmbedData
    {
        public List<double> Embedding { get; set; } = new();
    }

    private class OllamaEmbedRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
    }

    private class OllamaEmbedResponse
    {
        public List<List<double>> Embeddings { get; set; } = new();
    }

    private class AlibabaEmbedRequest
    {
        public string Model { get; set; } = string.Empty;
        public AlibabaEmbedInput Input { get; set; } = new();
    }

    private class AlibabaEmbedInput
    {
        public string Text { get; set; } = string.Empty;
    }

    private class AlibabaEmbedResponse
    {
        public AlibabaEmbedOutput? Output { get; set; }
    }

    private class AlibabaEmbedOutput
    {
        public List<double> Embedding { get; set; } = new();
    }

    private class BedrockEmbedRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class BedrockEmbedResponse
    {
        public BedrockEmbedOutput? Embedding { get; set; }
    }

    private class BedrockEmbedOutput
    {
        public List<double> Vector { get; set; } = new();
    }

    // Unified response type
    private class GenericEmbeddingResponse
    {
        public List<GenericEmbedData>? Data { get; set; }
        public List<List<double>>? Embeddings { get; set; }
        public GenericEmbedData? Output { get; set; }
        public BedrockEmbedOutput? Embedding { get; set; }
    }

    private class GenericEmbedData
    {
        public List<double> Embedding { get; set; } = new();
        public List<double>? Vector { get; set; }
    }
}
