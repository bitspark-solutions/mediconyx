namespace mediconyx_api.Models;

public class FileAttachment
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // File reference (from storage service)
    public string StorageFileId { get; set; } = string.Empty;

    // Link to entities
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public int UploadedByUserId { get; set; }

    // File metadata
    public string FileName { get; set; } = string.Empty;      // Stored filename (with extension)
    public string OriginalFileName { get; set; } = string.Empty; // User's original filename
    public string ContentType { get; set; } = string.Empty;    // MIME type
    public long FileSize { get; set; }

    // Categorization
    public FileAttachmentCategory Category { get; set; }

    // Optional description
    public string? Description { get; set; }

    // Internal
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
