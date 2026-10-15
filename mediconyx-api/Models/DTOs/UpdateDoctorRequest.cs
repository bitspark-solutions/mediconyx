namespace mediconyx_api.Models.DTOs;

public class UpdateDoctorRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? MedicalLicenseNumber { get; set; }
    public string? Specialization { get; set; }
    public int? DepartmentId { get; set; }
    public int? ConsultationDurationMinutes { get; set; }
    public decimal? ConsultationFee { get; set; }
    public bool? IsAcceptingPatients { get; set; }
    public bool? IsActive { get; set; }
}
