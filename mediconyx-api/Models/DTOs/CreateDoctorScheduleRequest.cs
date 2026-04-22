namespace mediconyx_api.Models.DTOs;

public class CreateDoctorScheduleRequest
{
    public int DoctorId { get; set; }
    public int DepartmentId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDurationMinutes { get; set; } = 30;
}
