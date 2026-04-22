namespace mediconyx_api.Models.DTOs;

public class UpdateDoctorScheduleRequest
{
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? SlotDurationMinutes { get; set; }
    public bool? IsActive { get; set; }
}
