namespace mediconyx_api.Models.DTOs;

public class FileAttachmentDto
{
    public int Id { get; set; }
    public string StorageFileId { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
}
