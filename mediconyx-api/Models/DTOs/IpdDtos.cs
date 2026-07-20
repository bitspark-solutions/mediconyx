using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

// ── Wards ─────────────────────────────────────────────────────

public class WardDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int Floor { get; set; }
    public int Capacity { get; set; }
    public decimal DefaultDailyRate { get; set; }
    public bool IsActive { get; set; }
    public int BedCount { get; set; }
    public int AvailableBedCount { get; set; }
    public int OccupiedBedCount { get; set; }
}

public class CreateWardRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(20)]  public string Code { get; set; } = string.Empty;
    [Required, StringLength(50)]  public string WardType { get; set; } = string.Empty;
    [StringLength(200)] public string? Location { get; set; }
    public int Floor { get; set; } = 1;
    public int Capacity { get; set; } = 0;
    public decimal DefaultDailyRate { get; set; } = 0m;
}

public class UpdateWardRequest
{
    [StringLength(100)] public string? Name { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    public int? Floor { get; set; }
    public int? Capacity { get; set; }
    public decimal? DefaultDailyRate { get; set; }
    public bool? IsActive { get; set; }
}

// ── Beds ──────────────────────────────────────────────────────

public class BedDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int WardId { get; set; }
    public string WardName { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? DailyRate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int? CurrentAdmissionId { get; set; }
    public int? CurrentPatientId { get; set; }
    public string? CurrentPatientName { get; set; }
    public string? CurrentPatientNumber { get; set; }
}

public class CreateBedRequest
{
    [Required] public int WardId { get; set; }
    [Required, StringLength(30)] public string Number { get; set; } = string.Empty;
    [StringLength(300)] public string? Notes { get; set; }
    public string Status { get; set; } = nameof(BedStatus.Available);
    public decimal? DailyRate { get; set; }
}

public class UpdateBedStatusRequest
{
    [Required] public string Status { get; set; } = string.Empty;
    [StringLength(300)] public string? Notes { get; set; }
    public decimal? DailyRate { get; set; }
}

// ── Admissions ──────────────────────────────────────────────

public class AdmissionDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientNumber { get; set; } = string.Empty;
    public int BedId { get; set; }
    public string BedNumber { get; set; } = string.Empty;
    public int WardId { get; set; }
    public string WardName { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public string AdmissionType { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? ExpectedDischargeDate { get; set; }
    public int AttendingDoctorId { get; set; }
    public string AttendingDoctorName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? DiagnosisOnAdmission { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DischargeDate { get; set; }
    public string? DischargeNotes { get; set; }
    public List<AdmissionTransferDto> Transfers { get; set; } = new();
    public DischargeSummaryDto? DischargeSummary { get; set; }
}

public class AdmissionTransferDto
{
    public int Id { get; set; }
    public int FromBedId { get; set; }
    public string FromBedNumber { get; set; } = string.Empty;
    public int ToBedId { get; set; }
    public string ToBedNumber { get; set; } = string.Empty;
    public DateTime TransferredAt { get; set; }
    public int TransferredByUserId { get; set; }
    public string? Reason { get; set; }
}

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

public class TransferAdmissionRequest
{
    [Required] public int ToBedId { get; set; }
    [StringLength(500)] public string? Reason { get; set; }
}

public class DischargeAdmissionRequest
{
    [Required] public int DischargingDoctorId { get; set; }
    [Required] public string ConditionAtDischarge { get; set; } = "Improved";
    [StringLength(2000)] public string? DischargeInstructions { get; set; }
    public DateTime? FollowUpDate { get; set; }
    [StringLength(2000)] public string? MedicationsOnDischarge { get; set; }
}

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

// ── Bed Board (composite view) ──────────────────────────────

public class BedBoardWardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public int Floor { get; set; }
    public List<BedBoardBedDto> Beds { get; set; } = new();
}

public class BedBoardBedDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? AdmissionId { get; set; }
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientNumber { get; set; }
    public string? AttendingDoctorName { get; set; }
    public DateTime? AdmissionDate { get; set; }
}
