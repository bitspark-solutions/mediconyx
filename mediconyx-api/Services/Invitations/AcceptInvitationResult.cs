using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Services.Invitations;

public class AcceptInvitationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? HospitalName { get; set; }
    /// <summary>True when the account is immediately usable (tenant self-service invite); false for pre-tenant pipeline invites.</summary>
    public bool AccountActivated { get; set; }
}
