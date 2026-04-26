namespace mediconyx_api.Services.AI;

public interface IStorageService
{
    Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string storageFileId, CancellationToken ct = default);
    Task<StorageFileMetadata?> GetMetadataAsync(string storageFileId, CancellationToken ct = default);
    Task<string> GenerateDownloadUrlAsync(string storageFileId, int expiryMinutes = 60, CancellationToken ct = default);
    Task<bool> DeleteAsync(string storageFileId, CancellationToken ct = default);
}
