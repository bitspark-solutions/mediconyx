using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class TransferAdmissionRequest
{
    [Required] public int ToBedId { get; set; }
    [StringLength(500)] public string? Reason { get; set; }
}
