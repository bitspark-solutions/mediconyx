using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

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
