namespace storage_service.Configuration;

public class StorageOptions
{
    public string BasePath { get; set; } = "/app/storage";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default
    public string[] AllowedMimeTypes { get; set; } = new[]
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // DOCX
        "application/msword", // DOC
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // XLSX
        "application/vnd.ms-excel", // XLS
        "image/jpeg",
        "image/png"
    };
    
    public int UrlExpiryMinutes { get; set; } = 60;
    public int SoftDeleteRetentionDays { get; set; } = 30;
}
