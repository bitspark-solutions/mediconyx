namespace storage_service.Models;

public class StorageResult
{
    public bool Success { get; set; }
    public string? FileId { get; set; }
    public string? ErrorMessage { get; set; }
    public FileMetadata? Metadata { get; set; }
}
