using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// Captured when a patient is discharged. Drives the discharge summary document
/// shown in the patient portal and the discharge follow-up email.
/// </summary>
public class DischargeSummary
{
    public int Id { get; set; }
    public int AdmissionId { get; set; }
    public Admission Admission { get; set; } = null!;

    public int DischargingDoctorId { get; set; }
    public DoctorProfile DischargingDoctor { get; set; } = null!;

    public DateTime DischargeDate { get; set; } = DateTime.UtcNow;

    public DischargeCondition ConditionAtDischarge { get; set; } = DischargeCondition.Improved;

    [StringLength(2000)]
    public string? DischargeInstructions { get; set; }

    public DateTime? FollowUpDate { get; set; }

    [StringLength(2000)]
    public string? MedicationsOnDischarge { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
