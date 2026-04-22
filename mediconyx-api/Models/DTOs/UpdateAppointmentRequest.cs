namespace mediconyx_api.Models.DTOs;

public class UpdateAppointmentRequest
{
    public int? DoctorId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public AppointmentType? Type { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
