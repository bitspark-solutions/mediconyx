namespace mediconyx_api.Models.DTOs;

public class AttachDoctorProfileRequest
{
    public int UserId { get; set; }
    public string? MedicalLicenseNumber { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int ConsultationDurationMinutes { get; set; } = 30;
    public decimal? ConsultationFee { get; set; }
}
