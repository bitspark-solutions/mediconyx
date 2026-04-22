namespace mediconyx_api.Models.DTOs;

public class CreateMedicalNoteRequest
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public MedicalNoteType NoteType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
}
