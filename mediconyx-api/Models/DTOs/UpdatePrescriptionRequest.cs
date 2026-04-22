namespace mediconyx_api.Models.DTOs;

public class UpdatePrescriptionRequest
{
    public string? Medications { get; set; }
    public string? Instructions { get; set; }
    public string? Allergies { get; set; }
}
