namespace mediconyx_api.Services.AI;

public class StorageUploadResult
{
    public bool Success { get; set; }
    public string? StorageFileId { get; set; }
    public string? ErrorMessage { get; set; }
    public long FileSize { get; set; }
    public string? SHA256 { get; set; }
}
