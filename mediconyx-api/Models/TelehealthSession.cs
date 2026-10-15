using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A telemedicine / video consultation session between a patient and a doctor.
/// MVP stub: stores session lifecycle (Scheduled → Active → Completed) and a
/// placeholder JoinUrl. Video transport is not implemented yet — when a real
/// provider (e.g. Twilio Video, Jitsi, Daily.co) is wired in, populate JoinUrl
/// with a real room URL.
/// </summary>
public class TelehealthSession
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public DoctorProfile Doctor { get; set; } = null!;

    /// <summary>Optional: link to a pre-booked Appointment.</summary>
    public int? AppointmentId { get; set; }

    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public TelehealthSessionStatus Status { get; set; } = TelehealthSessionStatus.Scheduled;

    [StringLength(500)]
    public string? JoinUrl { get; set; }   // e.g. https://meet.mediexample.com/room/abc123

    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>Token used to join the call without sharing credentials. MVP: just a random GUID.</summary>
    [StringLength(100)]
    public string? JoinToken { get; set; }

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
