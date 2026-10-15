using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class CreateWardRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(20)]  public string Code { get; set; } = string.Empty;
    [Required, StringLength(50)]  public string WardType { get; set; } = string.Empty;
    /// <summary>Branch the ward belongs to. Defaults to the tenant's main branch when omitted.</summary>
    public int? BranchId { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    public int Floor { get; set; } = 1;
    public int Capacity { get; set; } = 0;
    public decimal DefaultDailyRate { get; set; } = 0m;
}
