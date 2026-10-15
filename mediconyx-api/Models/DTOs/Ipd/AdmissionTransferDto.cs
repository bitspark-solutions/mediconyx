using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class AdmissionTransferDto
{
    public int Id { get; set; }
    public int FromBedId { get; set; }
    public string FromBedNumber { get; set; } = string.Empty;
    public int ToBedId { get; set; }
    public string ToBedNumber { get; set; } = string.Empty;
    public DateTime TransferredAt { get; set; }
    public int TransferredByUserId { get; set; }
    public string? Reason { get; set; }
}
