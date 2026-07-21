using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A physical ward within a hospital. Each ward has a type (ICU, PICU, DayCare, etc.)
/// and contains zero-or-more Beds. Wards are tenant-scoped — each hospital defines its own.
/// </summary>
public class Ward
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code for display (e.g. "ICU-1", "PICU-A", "DC-PED").</summary>
    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Type of ward. Matches the broader ServiceTypes taxonomy so a HospitalService of
    /// type ICU can be linked to a Ward of type ICU for billing/charge lookups.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string WardType { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }  // e.g. "Block B, Floor 3, Wing East"

    public int Floor { get; set; } = 1;

    /// <summary>Total bed count (informational; actual bed count is in the Bed table).</summary>
    public int Capacity { get; set; }

    /// <summary>Default per-day charge (BDT) for any bed in this ward. Bed.DailyRate overrides this when set.</summary>
    public decimal DefaultDailyRate { get; set; } = 0m;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Bed> Beds { get; set; } = new List<Bed>();
}

public static class WardTypes
{
    public const string General = "General";
    public const string ICU = "ICU";
    public const string PICU = "PICU";
    public const string NICU = "NICU";
    public const string DayCare = "DayCare";
    public const string Maternity = "Maternity";
    public const string Pediatric = "Pediatric";
    public const string Geriatric = "Geriatric";
    public const string Surgery = "Surgery";
    public const string Cabin = "Cabin";

    public static readonly string[] All = {
        General, ICU, PICU, NICU, DayCare, Maternity, Pediatric, Geriatric, Surgery, Cabin
    };
}
