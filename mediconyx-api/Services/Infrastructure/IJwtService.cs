using mediconyx_api.Models;

namespace mediconyx_api.Services.Infrastructure;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    (string AccessToken, string RefreshToken) GenerateTokenPair(User user);
}
