using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class UpdateBedStatusRequest
{
    [Required] public string Status { get; set; } = string.Empty;
    [StringLength(300)] public string? Notes { get; set; }
    public decimal? DailyRate { get; set; }
    [StringLength(50)] public string? CabinType { get; set; }
    public string[]? Facilities { get; set; }
}
