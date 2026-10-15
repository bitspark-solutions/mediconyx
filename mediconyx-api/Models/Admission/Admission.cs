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

    /// <summary>Branch of the current bed. Denormalized for simple filtering; updated on transfer.</summary>
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

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
