using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class UpdateWardRequest
{
    [StringLength(100)] public string? Name { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    public int? Floor { get; set; }
    public int? Capacity { get; set; }
    public decimal? DefaultDailyRate { get; set; }
    public bool? IsActive { get; set; }
}
