using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Middleware;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Auth;

/// <summary>
/// ASP.NET Core authorization handler. Reads permission claims directly from the
/// user's JWT (populated at login by JwtService) — no DB hit per request.
/// Singleton because it has no state.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<HasPermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasPermissionRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true) return Task.CompletedTask;

        // Master always has every permission
        if (user.IsInRole(UserRole.Master.ToString()) || user.HasClaim("permission", "all"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Direct permission match
        if (user.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
