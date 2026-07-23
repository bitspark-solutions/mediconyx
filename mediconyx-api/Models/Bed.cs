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

    /// <summary>Branch (hospital location) this bed belongs to. Denormalized from Ward for simple filtering.</summary>
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int WardId { get; set; }
    public Ward Ward { get; set; } = null!;

    /// <summary>Bed number/code within its ward. e.g. "B-101", "PICU-A-3", "CAB-201".</summary>
    [Required]
    [StringLength(30)]
    public string Number { get; set; } = string.Empty;

    /// <summary>Whether this unit is a shared ward bed or a private cabin.</summary>
    public BedKind Kind { get; set; } = BedKind.Bed;

    /// <summary>
    /// Cabin category when Kind = Cabin (see <see cref="CabinTypes"/>). Null for regular beds.
    /// e.g. "Standard", "Deluxe", "VIP", "Suite".
    /// </summary>
    [StringLength(50)]
    public string? CabinType { get; set; }

    /// <summary>
    /// JSON array of amenity names for cabins, e.g. ["AC","Attached Bathroom","TV","Attendant Bed"].
    /// Defaults come from <see cref="CabinTypes.DefaultFacilities"/> when not specified at creation.
    /// Null/empty for regular beds.
    /// </summary>
    [StringLength(1000)]
    public string? Facilities { get; set; }

    public BedStatus Status { get; set; } = BedStatus.Available;

    /// <summary>Per-day charge (BDT) for this specific bed. Null = inherit from ward's DefaultDailyRate.</summary>
    public decimal? DailyRate { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

