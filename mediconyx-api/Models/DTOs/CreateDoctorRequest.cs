namespace mediconyx_api.Models.DTOs;

public class CreateDoctorRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? MedicalLicenseNumber { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int ConsultationDurationMinutes { get; set; } = 30;
    public decimal? ConsultationFee { get; set; }
}
