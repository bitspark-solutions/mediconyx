namespace mediconyx_api.Models.DTOs;

public class CreateAppointmentRequest
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int DepartmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public AppointmentType Type { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
