namespace mediconyx_api.Services;

public interface ICaptchaService
{
    Task<bool> VerifyTokenAsync(string token, string? remoteIp = null);
}
