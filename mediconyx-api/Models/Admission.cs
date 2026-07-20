using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A patient admission to a bed. One Admission = one patient in one bed over a date range.
/// Transfers are recorded separately in AdmissionTransfer (history).
/// </summary>
public class Admission
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int BedId { get; set; }
    public Bed Bed { get; set; } = null!;

    public AdmissionType AdmissionType { get; set; } = AdmissionType.Elective;

    public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDischargeDate { get; set; }

    /// <summary>Doctor responsible for the patient's care during this admission.</summary>
    public int AttendingDoctorId { get; set; }
    public DoctorProfile AttendingDoctor { get; set; } = null!;

    [StringLength(1000)]
    public string? Reason { get; set; }

    [StringLength(2000)]
    public string? DiagnosisOnAdmission { get; set; }

    public AdmissionStatus Status { get; set; } = AdmissionStatus.Active;

    public DateTime? DischargeDate { get; set; }
    [StringLength(2000)]
    public string? DischargeNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AdmissionTransfer> Transfers { get; set; } = new List<AdmissionTransfer>();
    public DischargeSummary? DischargeSummary { get; set; }
}

public enum AdmissionType
{
    Emergency = 0,
    Elective = 1,
    Transfer = 2,
    DayCare = 3
}

public enum AdmissionStatus
{
    Active = 0,
    Discharged = 1,
    Transferred = 2,    // patient moved to another facility
    LAMA = 3,           // Leave Against Medical Advice
    Deceased = 4
}

/// <summary>
/// One row per bed move within the same admission. e.g. patient moves from ICU to General.
/// </summary>
public class AdmissionTransfer
{
    public int Id { get; set; }
    public int AdmissionId { get; set; }
    public Admission Admission { get; set; } = null!;

    public int FromBedId { get; set; }
    public Bed FromBed { get; set; } = null!;

    public int ToBedId { get; set; }
    public Bed ToBed { get; set; } = null!;

    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    public int TransferredByUserId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

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

public enum DischargeCondition
{
    Improved = 0,
    Stable = 1,
    Critical = 2,
    LAMA = 3,
    Deceased = 4
}
