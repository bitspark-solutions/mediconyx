using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public class DoctorProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [StringLength(100)]
    public string? MedicalLicenseNumber { get; set; }

    [StringLength(100)]
    public string Specialization { get; set; } = string.Empty;  // General, Cardiology, Orthopedics, Neurology, etc.

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int ConsultationDurationMinutes { get; set; } = 30;

    public decimal? ConsultationFee { get; set; }

    public bool IsAcceptingPatients { get; set; } = true;

    public bool IsActive { get; set; } = true;

    // ── Public-profile fields (Tier 3.4) ──
    [StringLength(2000)]
    public string? Bio { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public int YearsOfExperience { get; set; } = 0;

    /// <summary>JSON array of languages, e.g. ["English","Bengali","Hindi"].</summary>
    [StringLength(500)]
    public string? LanguagesJson { get; set; }

    /// <summary>JSON array of qualifications, e.g. ["MBBS","FCPS (Cardiology)","MD"].</summary>
    [StringLength(1000)]
    public string? QualificationsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
