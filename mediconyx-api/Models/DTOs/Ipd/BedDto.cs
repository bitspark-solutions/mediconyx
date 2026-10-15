using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

// ── Beds ──────────────────────────────────────────────────────

public class BedDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int BranchId { get; set; }
    public int WardId { get; set; }
    public string WardName { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Kind { get; set; } = nameof(BedKind.Bed);
    public string? CabinType { get; set; }
    public string? Facilities { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? DailyRate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int? CurrentAdmissionId { get; set; }
    public int? CurrentPatientId { get; set; }
    public string? CurrentPatientName { get; set; }
    public string? CurrentPatientNumber { get; set; }
}
