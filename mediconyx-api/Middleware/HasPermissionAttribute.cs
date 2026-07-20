using Microsoft.AspNetCore.Authorization;

namespace mediconyx_api.Middleware;

/// <summary>
/// Apply to a controller action (or whole controller) to require a specific permission.
/// The permission check reads from the user's JWT claims, which are populated at login
/// from <see cref="Models.RolePermission"/> rows.
/// </summary>
/// <example>
/// [HasPermission(PermissionCatalog.ADMISSION_CREATE)]
/// public async Task&lt;ActionResult&gt; AdmitPatient(...) { ... }
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute, IAuthorizationRequirement
{
    public string Permission { get; }

    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
        Permission = permission;
    }
}
