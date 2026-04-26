using System.Security.Claims;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Common;

public static class ClaimsHelper
{
    public static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    public static int? GetTenantId(ClaimsPrincipal user)
    {
        var value = user.FindFirst("TenantId")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    public static UserRole GetRole(ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(value, out var role) ? role : UserRole.Patient;
    }
}
