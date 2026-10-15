namespace mediconyx_api.Models.DTOs;

/// <summary>
/// Public-safe doctor profile view. Excludes phone, email, license number, and salary.
/// Used by the public /doctors listing and /doctors/{id} pages (no auth).
/// </summary>
public class PublicDoctorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;       // "Dr. First Last"
    public string Title { get; set; } = string.Empty;           // User.Designation
    public string Specialization { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int? DepartmentId { get; set; }
    public string? Bio { get; set; }
    public int YearsOfExperience { get; set; }
    public List<string> Languages { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();
    public decimal? ConsultationFee { get; set; }
    public string FeeCurrency { get; set; } = "BDT";
    public int ConsultationDurationMinutes { get; set; }
    public bool IsAcceptingPatients { get; set; }
    public string? PhotoUrl { get; set; }

    // Hospital context
    public int TenantId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string? HospitalLogoUrl { get; set; }
    public string? HospitalSubdomain { get; set; }
}
