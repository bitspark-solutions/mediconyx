using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A single physical bed. Status drives the Bed Board UI: Available (green),
/// Occupied (red with patient name), Cleaning (yellow), Maintenance (gray), Reserved (blue).
/// </summary>
public class Bed
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public int WardId { get; set; }
    public Ward Ward { get; set; } = null!;

    /// <summary>Bed number/code within its ward. e.g. "B-101", "PICU-A-3".</summary>
    [Required]
    [StringLength(30)]
    public string Number { get; set; } = string.Empty;

    public BedStatus Status { get; set; } = BedStatus.Available;

    [StringLength(300)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum BedStatus
{
    Available = 0,
    Occupied = 1,
    Cleaning = 2,
    Maintenance = 3,
    Reserved = 4
}
