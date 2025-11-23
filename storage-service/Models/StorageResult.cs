namespace storage_service.Models;

public class StorageResult
{
    public bool Success { get; set; }
    public string? FileId { get; set; }
    public string? ErrorMessage { get; set; }
    public FileMetadata? Metadata { get; set; }
}

public class UploadResult
{
    public string FileId { get; set; } = string.Empty;
    public string SHA256 { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long SizeBytes { get; set; }
}

public class UrlResult
{
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
