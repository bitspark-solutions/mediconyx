using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using storage_service.Configuration;

namespace storage_service.Services;

/// <summary>
/// HMAC-SHA256 signed download tokens. Token format (base64url):
///   {fileId}|{expiryUnix}|{hmacHex}
/// where hmac = HMACSHA256("{fileId}|{expiryUnix}", signingKey).
/// The signing key is the shared Storage:ApiKey; a fixed dev fallback is used when
/// no key is configured so the flow still works locally (with a startup warning).
/// </summary>
public class UrlSigningService
{
    private const string DevFallbackKey = "dev-only-signing-key-not-for-production";
    private readonly string _signingKey;
    private readonly ILogger<UrlSigningService> _logger;

    public UrlSigningService(IOptions<StorageOptions> options, ILogger<UrlSigningService> logger)
    {
        _logger = logger;
        _signingKey = string.IsNullOrWhiteSpace(options.Value.ApiKey)
            ? DevFallbackKey
            : options.Value.ApiKey;
    }

    public string Sign(string fileId, long expiryUnix)
    {
        var payload = $"{fileId}|{expiryUnix}";
        var hmac = ComputeHmac(payload);
        var token = $"{payload}|{hmac}";
        return Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    /// <summary>True when the token is well-formed, correctly signed, and not expired.</summary>
    public bool Verify(string fileId, string token)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Base64UrlDecode(token));
            var parts = decoded.Split('|');
            if (parts.Length != 3) return false;
            if (parts[0] != fileId) return false;
            if (!long.TryParse(parts[1], out var expiryUnix)) return false;
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiryUnix) return false;

            var expected = ComputeHmac($"{parts[0]}|{parts[1]}");
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected),
                Encoding.ASCII.GetBytes(parts[2]));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Signed URL verification failed for file {FileId}", fileId);
            return false;
        }
    }

    private string ComputeHmac(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signingKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
}
