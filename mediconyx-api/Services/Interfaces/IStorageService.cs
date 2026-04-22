namespace mediconyx_api.Services.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Upload a file to storage and return the storage file ID
    /// </summary>
    Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Get a download stream for a file
    /// </summary>
    Task<Stream?> DownloadAsync(string storageFileId, CancellationToken ct = default);

    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<StorageFileMetadata?> GetMetadataAsync(string storageFileId, CancellationToken ct = default);

    /// <summary>
    /// Generate a presigned URL for file download
    /// </summary>
    Task<string> GenerateDownloadUrlAsync(string storageFileId, int expiryMinutes = 60, CancellationToken ct = default);

    /// <summary>
    /// Delete a file (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(string storageFileId, CancellationToken ct = default);
}

public class StorageUploadResult
{
    public bool Success { get; set; }
    public string? StorageFileId { get; set; }
    public string? ErrorMessage { get; set; }
    public long FileSize { get; set; }
    public string? SHA256 { get; set; }
}

public class StorageFileMetadata
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string SHA256 { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
}
