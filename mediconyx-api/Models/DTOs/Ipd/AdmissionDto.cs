using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

// ── Admissions ──────────────────────────────────────────────

public class AdmissionDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
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
