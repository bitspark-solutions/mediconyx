using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class VerifyEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? TenantName { get; set; }
    public AuthResponse? AuthResponse { get; set; }   // populated when verification auto-logs the user in
}
