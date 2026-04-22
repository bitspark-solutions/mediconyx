namespace mediconyx_api.Models;

public class MedicalNote
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public int AuthorUserId { get; set; }

    public MedicalNoteType NoteType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum MedicalNoteType
{
    Consultation = 0,
    FollowUp = 1,
    Procedure = 2,
    Discharge = 3,
    Referral = 4,
    General = 5
}
