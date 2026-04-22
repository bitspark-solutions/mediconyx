namespace mediconyx_api.Models.DTOs;

public class CreatePrescriptionRequest
{
    public int PatientId { get; set; }
    public int AppointmentId { get; set; }
    public string Medications { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string? Allergies { get; set; }
}
