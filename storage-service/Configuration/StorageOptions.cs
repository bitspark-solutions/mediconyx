namespace storage_service.Configuration;

public class StorageOptions
{
    public string BasePath { get; set; } = "/app/storage";
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB — aligned with the API's FileAttachment policy
    public string[] AllowedMimeTypes { get; set; } = new[]
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // DOCX
        "application/msword", // DOC
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // XLSX
        "application/vnd.ms-excel", // XLS
        "image/jpeg",
        "image/png",
        "image/gif",
        "application/dicom"
    };

    /// <summary>
    /// Shared secret the main API must send as X-Api-Key. Empty = auth disabled (local dev only —
    /// a warning is logged at startup). Always set this in deployed environments.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    public int UrlExpiryMinutes { get; set; } = 60;
    public int SoftDeleteRetentionDays { get; set; } = 30;

    /// <summary>How often the soft-delete purge job runs (hours).</summary>
    public int PurgeIntervalHours { get; set; } = 6;
}
