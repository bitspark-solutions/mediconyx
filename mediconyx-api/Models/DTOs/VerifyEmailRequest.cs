using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class VerifyEmailRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
