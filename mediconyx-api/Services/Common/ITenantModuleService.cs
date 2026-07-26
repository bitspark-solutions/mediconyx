namespace mediconyx_api.Services.Common;

public interface ITenantModuleService
{
    /// <summary>True when the given module flag is enabled for the tenant.</summary>
    Task<bool> IsEnabledAsync(int tenantId, string moduleKey);

    /// <summary>Throws <see cref="Middleware.ModuleDisabledException"/> when the module is disabled.</summary>
    Task RequireEnabledAsync(int tenantId, string moduleKey, string displayName);
}
