using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class CreateBedRequest
{
    [Required] public int WardId { get; set; }
    /// <summary>Must match the ward's branch when provided; otherwise inherited from the ward.</summary>
    public int? BranchId { get; set; }
    [Required, StringLength(30)] public string Number { get; set; } = string.Empty;
    [StringLength(300)] public string? Notes { get; set; }
    public string Status { get; set; } = nameof(BedStatus.Available);
    public decimal? DailyRate { get; set; }
    public string Kind { get; set; } = nameof(BedKind.Bed);
    [StringLength(50)] public string? CabinType { get; set; }
    /// <summary>Optional amenity list; defaults from CabinTypes.DefaultFacilities when omitted for cabins.</summary>
    public string[]? Facilities { get; set; }
}
