using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models.DTOs;

public class TelehealthSessionDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientNumber { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? JoinUrl { get; set; }
    public string? JoinToken { get; set; }
    public string? Notes { get; set; }
    public int DurationMinutes => EndedAt.HasValue && StartedAt.HasValue
        ? (int)Math.Round((EndedAt.Value - StartedAt.Value).TotalMinutes)
        : 0;
    public DateTime CreatedAt { get; set; }
}

public class CreateTelehealthSessionRequest
{
    [Required] public int PatientId { get; set; }
    [Required] public int DoctorId { get; set; }
    public int? AppointmentId { get; set; }
    [Required] public DateTime ScheduledAt { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
}
