using mediconyx_api.Models;
using mediconyx_api.Services.Common;

namespace mediconyx_api.Middleware;

/// <summary>
/// Gates API route prefixes behind tenant module toggles. Master bypasses (platform
/// operations shouldn't be blocked by a tenant's module flags); everyone else gets
/// a 403 when their hospital has the module disabled.
/// </summary>
public class ModuleEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ModuleEnforcementMiddleware> _logger;

    /// <summary>Route prefix → (module key, display name).</summary>
    private static readonly (string Prefix, string Key, string Name)[] RouteModules =
    {
        ("/api/ipd", ModuleKeys.Ipd, "IPD"),
        ("/api/appointment", ModuleKeys.Appointment, "Appointment Scheduling"),
        ("/api/telemedicine", ModuleKeys.Telemedicine, "Telemedicine"),
        ("/api/invoice", ModuleKeys.Billing, "Billing & Invoicing"),
    };

    public ModuleEnforcementMiddleware(RequestDelegate next, ILogger<ModuleEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantModuleService moduleService)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var rule = RouteModules.FirstOrDefault(r =>
            path.StartsWith(r.Prefix, StringComparison.OrdinalIgnoreCase));

        if (rule != default && context.User.Identity?.IsAuthenticated == true
            && !context.User.IsInRole(UserRole.Master.ToString()))
        {
            var tenantId = Services.Common.ClaimsHelper.GetTenantId(context.User);
            if (tenantId != null && !await moduleService.IsEnabledAsync(tenantId.Value, rule.Key))
            {
                _logger.LogInformation("Blocked {Path} — module {Module} disabled for tenant {TenantId}",
                    path, rule.Key, tenantId);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = $"The {rule.Name} module is not enabled for this hospital. Contact the platform administrator."
                });
                return;
            }
        }

        await _next(context);
    }
}
