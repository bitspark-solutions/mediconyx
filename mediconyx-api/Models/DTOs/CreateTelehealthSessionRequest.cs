using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models.DTOs;

public class CreateTelehealthSessionRequest
{
    [Required] public int PatientId { get; set; }
    [Required] public int DoctorId { get; set; }
    public int? AppointmentId { get; set; }
    [Required] public DateTime ScheduledAt { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
}
