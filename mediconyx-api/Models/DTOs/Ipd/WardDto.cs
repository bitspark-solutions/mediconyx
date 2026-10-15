using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

// ── Wards ─────────────────────────────────────────────────────

public class WardDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
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
