using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A physical branch/location of a hospital (Tenant). Max Healthcare model: one brand,
/// many units — patients and staff belong to the tenant, operations (wards, beds,
/// admissions, appointments, billing) are scoped to a branch.
/// Every tenant has exactly one branch with IsMain = true (created by backfill for
/// pre-branch data; new tenants get one at provisioning or first branch creation).
/// </summary>
public class Branch
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code unique per tenant, e.g. "MAIN", "UTTARA", "DHANMONDI".</summary>
    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    /// <summary>The primary/home branch of the tenant. Exactly one per tenant.</summary>
    public bool IsMain { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
