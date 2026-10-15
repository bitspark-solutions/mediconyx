using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Middleware;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Auth;

/// <summary>Marker requirement class for the policy system.</summary>
public class HasPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public HasPermissionRequirement(string permission) => Permission = permission;
}
