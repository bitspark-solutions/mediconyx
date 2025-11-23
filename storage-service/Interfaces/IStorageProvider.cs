using storage_service.Models;

namespace storage_service.Interfaces;

public interface IStorageProvider
{
    Task<StorageResult> UploadAsync(Stream content, string fileName, string mimeType);
    Task<Stream?> DownloadAsync(string fileId);
    Task<bool> DeleteAsync(string fileId);
    Task<FileMetadata?> GetMetadataAsync(string fileId);
    Task<bool> ExistsAsync(string fileId);
    Task<string> GenerateUrlAsync(string fileId, int expiryMinutes);
}
