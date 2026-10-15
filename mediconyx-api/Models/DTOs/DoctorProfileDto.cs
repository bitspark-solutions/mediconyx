namespace mediconyx_api.Models.DTOs;

public class DoctorProfileDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? MedicalLicenseNumber { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int ConsultationDurationMinutes { get; set; }
    public decimal? ConsultationFee { get; set; }
    public bool IsAcceptingPatients { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
