namespace mediconyx_api.Models;

public class SalesRequestAttachment
{
    public int Id { get; set; }
    public int SalesRequestId { get; set; }
    public SalesRequest SalesRequest { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
