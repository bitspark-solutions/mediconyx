using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models.DTOs;

public class UpdateBranchRequest
{
    [StringLength(100)] public string? Name { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(150)] public string? Email { get; set; }
    public bool? IsMain { get; set; }
    public bool? IsActive { get; set; }
}
