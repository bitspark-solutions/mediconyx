namespace mediconyx_api.Models;

public class Appointment
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>Branch (hospital location) where the appointment takes place. Multi-branch (Tier 4.3).</summary>
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    // References — simple int IDs, no navigation properties
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int DepartmentId { get; set; }

    // Schedule
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Details
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentType Type { get; set; } = AppointmentType.Consultation;
    public string? Reason { get; set; }
    public string? Notes { get; set; }

    // Cancellation
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    // Check-in tracking
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Audit
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
