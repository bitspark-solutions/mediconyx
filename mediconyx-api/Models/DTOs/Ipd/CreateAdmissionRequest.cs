using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class CreateAdmissionRequest
{
    [Required] public int PatientId { get; set; }
    [Required] public int BedId { get; set; }
    [Required] public int AttendingDoctorId { get; set; }
    [Required] public string AdmissionType { get; set; } = "Elective";
    public DateTime? ExpectedDischargeDate { get; set; }
    [StringLength(1000)] public string? Reason { get; set; }
    [StringLength(2000)] public string? DiagnosisOnAdmission { get; set; }
}
