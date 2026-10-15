using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class DischargeAdmissionRequest
{
    [Required] public int DischargingDoctorId { get; set; }
    [Required] public string ConditionAtDischarge { get; set; } = "Improved";
    [StringLength(2000)] public string? DischargeInstructions { get; set; }
    public DateTime? FollowUpDate { get; set; }
    [StringLength(2000)] public string? MedicationsOnDischarge { get; set; }
}
