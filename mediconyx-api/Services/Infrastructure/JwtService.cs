using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Infrastructure;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user) => GenerateAccessToken(user, Array.Empty<string>());

    public string GenerateAccessToken(User user, IEnumerable<string> permissions)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = jwtSettings["Issuer"] ?? "Mediconyx";
        var audience = jwtSettings["Audience"] ?? "MediconyxAPI";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("TenantId", user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Optional: the user's home branch (multi-branch Stage 3). Old tokens simply
        // lack this claim until the user logs in again — same as role claims.
        if (user.PrimaryBranchId.HasValue)
            claims.Add(new Claim("branch_id", user.PrimaryBranchId.Value.ToString()));

        // One claim per permission — checked via PermissionService.HasPermission.
        // For Master, also include "all" so downstream services can short-circuit.
        if (user.Role == UserRole.Master)
        {
            claims.Add(new Claim("permission", "all"));
        }
        foreach (var p in permissions ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(p))
                claims.Add(new Claim("permission", p));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public (string AccessToken, string RefreshToken) GenerateTokenPair(User user)
        => (GenerateAccessToken(user), GenerateRefreshToken());
}
