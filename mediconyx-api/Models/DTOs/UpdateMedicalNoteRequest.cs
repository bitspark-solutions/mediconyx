namespace mediconyx_api.Models.DTOs;

public class UpdateMedicalNoteRequest
{
    public MedicalNoteType? NoteType { get; set; }
    public string? Content { get; set; }
    public string? Diagnosis { get; set; }
}
