using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class RegisterPatientResponse
{
    public string Message { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public bool RequiresEmailVerification { get; set; }
}
