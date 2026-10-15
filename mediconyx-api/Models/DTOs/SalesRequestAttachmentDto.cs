namespace mediconyx_api.Models.DTOs;

public class SalesRequestAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? DocumentType { get; set; }
    public bool IsMandatory { get; set; }
    public DateTime UploadedAt { get; set; }
}
