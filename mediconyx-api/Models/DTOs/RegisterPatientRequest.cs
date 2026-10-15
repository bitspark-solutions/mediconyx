using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

// ── Public patient self-registration (no auth, captcha-protected) ──

public class RegisterPatientRequest
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public string Gender { get; set; } = "Male";   // "Male" | "Female" | "Other"

    /// <summary>
    /// Tenant subdomain (e.g. "citygeneral") of the hospital this patient wants to register against.
    /// If null/empty, the patient registers against the platform's default tenant.
    /// </summary>
    [StringLength(100)]
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Optional registration code (e.g. printed on a flyer at a hospital). When supplied,
    /// the patient joins the tenant that owns that code. Falls back to TenantSubdomain.
    /// </summary>
    [StringLength(50)]
    public string? RegistrationCode { get; set; }

    /// <summary>reCAPTCHA token from the client. Bypassed in dev/test if no secret configured.</summary>
    public string? CaptchaToken { get; set; }
}
