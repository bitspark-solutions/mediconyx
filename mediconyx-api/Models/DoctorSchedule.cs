namespace mediconyx_api.Models;

public class DoctorSchedule
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int DoctorId { get; set; }
    public int DepartmentId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
