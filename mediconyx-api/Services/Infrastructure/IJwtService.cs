using mediconyx_api.Models;

namespace mediconyx_api.Services.Infrastructure;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateAccessToken(User user, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    (string AccessToken, string RefreshToken) GenerateTokenPair(User user);
}
