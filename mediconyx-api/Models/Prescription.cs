namespace mediconyx_api.Models;

public class Prescription
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public int AppointmentId { get; set; }
    public int DoctorId { get; set; }

    // Medications stored as plain text (one line per medication)
    // Format: "MedicationName | Dosage | Frequency | Duration"
    public string Medications { get; set; } = string.Empty;

    public string? Instructions { get; set; }
    public string? Allergies { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
