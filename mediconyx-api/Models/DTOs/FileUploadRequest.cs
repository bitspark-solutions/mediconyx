namespace mediconyx_api.Models.DTOs;

public class FileUploadRequest
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public FileAttachmentCategory Category { get; set; }
    public string? Description { get; set; }
}
