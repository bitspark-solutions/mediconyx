using System.Text.Json;

namespace mediconyx_api.Services;

public class RecaptchaService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly ILogger<RecaptchaService> _logger;
    private readonly bool _isDevelopment;

    public RecaptchaService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<RecaptchaService> logger,
        IWebHostEnvironment environment)
    {
        _httpClient = httpClient;
        _secretKey = configuration["Recaptcha:SecretKey"] ?? string.Empty;
        _logger = logger;
        _isDevelopment = environment.IsDevelopment();
    }

    public async Task<bool> VerifyTokenAsync(string token, string? remoteIp = null)
    {
        // In development, bypass captcha verification if no secret key is set
        if (_isDevelopment && string.IsNullOrWhiteSpace(_secretKey))
        {
            _logger.LogWarning("Captcha verification bypassed in development mode (no secret key configured)");
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "secret", _secretKey },
                { "response", token }
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                parameters.Add("remoteip", remoteIp);
            }

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString);
            
            if (result?.Success == true)
            {
                return true;
            }

            _logger.LogWarning("Captcha verification failed: {Errors}", string.Join(", ", result?.ErrorCodes ?? Array.Empty<string>()));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying captcha token");
            return false;
        }
    }

    private class RecaptchaResponse
    {
        public bool Success { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string? Hostname { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
