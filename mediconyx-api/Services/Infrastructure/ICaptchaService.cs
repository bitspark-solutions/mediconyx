namespace mediconyx_api.Services.Infrastructure;

public interface ICaptchaService
{
    Task<bool> VerifyTokenAsync(string token, string? remoteIp = null);
}
