using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

// ── Bed Board (composite view) ──────────────────────────────

public class BedBoardWardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public int Floor { get; set; }
    public List<BedBoardBedDto> Beds { get; set; } = new();
}
