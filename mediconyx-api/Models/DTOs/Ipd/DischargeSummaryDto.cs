using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class DischargeSummaryDto
{
    public int Id { get; set; }
    public int AdmissionId { get; set; }
    public int DischargingDoctorId { get; set; }
    public string DischargingDoctorName { get; set; } = string.Empty;
    public DateTime DischargeDate { get; set; }
    public string ConditionAtDischarge { get; set; } = string.Empty;
    public string? DischargeInstructions { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public string? MedicationsOnDischarge { get; set; }
}
