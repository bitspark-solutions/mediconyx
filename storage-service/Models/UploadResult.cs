namespace storage_service.Models;

public class UploadResult
{
    public string FileId { get; set; } = string.Empty;
    public string SHA256 { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long SizeBytes { get; set; }
}
