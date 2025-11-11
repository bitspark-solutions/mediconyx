using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

