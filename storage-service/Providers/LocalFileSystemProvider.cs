using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using storage_service.Configuration;
using storage_service.Interfaces;
using storage_service.Models;
using storage_service.Services;

namespace storage_service.Providers;

public class LocalFileSystemProvider : IStorageProvider
{
    private readonly StorageOptions _options;
    private readonly ILogger<LocalFileSystemProvider> _logger;
    private readonly UrlSigningService _urlSigning;

    public LocalFileSystemProvider(
        IOptions<StorageOptions> options,
        ILogger<LocalFileSystemProvider> logger,
        UrlSigningService urlSigning)
    {
        _options = options.Value;
        _logger = logger;
        _urlSigning = urlSigning;
        
        // Ensure base storage directory exists
        if (!Directory.Exists(_options.BasePath))
        {
            Directory.CreateDirectory(_options.BasePath);
            _logger.LogInformation("Created storage directory: {Path}", _options.BasePath);
        }
    }

    public async Task<StorageResult> UploadAsync(Stream content, string fileName, string mimeType)
    {
        try
        {
            // Generate unique file ID
            var fileId = Guid.NewGuid().ToString("N");
            
            // Get file extension from filename
            var extension = Path.GetExtension(fileName);
            
            // Create date-based directory structure
            var now = DateTime.UtcNow;
            var yearPath = Path.Combine(_options.BasePath, now.Year.ToString());
            var monthPath = Path.Combine(yearPath, now.Month.ToString("00"));
            var dayPath = Path.Combine(monthPath, now.Day.ToString("00"));
            
            // Ensure directory exists
            Directory.CreateDirectory(dayPath);
            
            // Build file path
            var filePath = Path.Combine(dayPath, $"{fileId}{extension}");
            var metadataPath = Path.Combine(dayPath, $"{fileId}.meta.json");
            
            // Calculate SHA256 hash while writing file
            string hash;
            long fileSize;
            
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var sha256 = SHA256.Create())
            {
                var buffer = new byte[8192];
                int bytesRead;
                
                while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
                
                sha256.TransformFinalBlock(new byte[0], 0, 0);
                hash = BitConverter.ToString(sha256.Hash!).Replace("-", "").ToLowerInvariant();
                fileSize = fileStream.Length;
            }
            
            // Create metadata
            var metadata = new FileMetadata
            {
                FileId = fileId,
                FileName = fileName,
                MimeType = mimeType,
                SizeBytes = fileSize,
                SHA256 = hash,
                UploadedAt = now,
                IsDeleted = false
            };
            
            // Save metadata
            var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(metadataPath, metadataJson);
            
            _logger.LogInformation(
                "File uploaded successfully. FileId: {FileId}, Size: {Size} bytes, SHA256: {Hash}",
                fileId, fileSize, hash);
            
            return new StorageResult
            {
                Success = true,
                FileId = fileId,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            return new StorageResult
            {
                Success = false,
                ErrorMessage = $"Upload failed: {ex.Message}"
            };
        }
    }

    public async Task<Stream?> DownloadAsync(string fileId)
    {
        try
        {
            var filePath = await FindFilePathAsync(fileId);
            
            if (filePath == null)
            {
                _logger.LogWarning("File not found: {FileId}", fileId);
                return null;
            }
            
            // Check if file is deleted
            var metadata = await GetMetadataAsync(fileId);
            if (metadata?.IsDeleted == true)
            {
                _logger.LogWarning("Attempted to download deleted file: {FileId}", fileId);
                return null;
            }
            
            // Return file stream
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileId}", fileId);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string fileId)
    {
        try
        {
            var metadata = await GetMetadataAsync(fileId);
            
            if (metadata == null)
            {
                _logger.LogWarning("Cannot delete - file not found: {FileId}", fileId);
                return false;
            }
            
            // Soft delete: Update metadata
            metadata.IsDeleted = true;
            metadata.DeletedAt = DateTime.UtcNow;
            
            var metadataPath = await FindMetadataPathAsync(fileId);
            if (metadataPath != null)
            {
                var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(metadataPath, metadataJson);
                
                _logger.LogInformation("File soft-deleted: {FileId}", fileId);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileId}", fileId);
            return false;
        }
    }

    public async Task<FileMetadata?> GetMetadataAsync(string fileId)
    {
        try
        {
            var metadataPath = await FindMetadataPathAsync(fileId);
            
            if (metadataPath == null || !File.Exists(metadataPath))
            {
                return null;
            }
            
            var metadataJson = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<FileMetadata>(metadataJson);
            
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading metadata: {FileId}", fileId);
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string fileId)
    {
        var filePath = await FindFilePathAsync(fileId);
        return filePath != null && File.Exists(filePath);
    }

    public async Task<string> GenerateUrlAsync(string fileId, int expiryMinutes)
    {
        var metadata = await GetMetadataAsync(fileId);

        if (metadata == null)
        {
            throw new FileNotFoundException($"File not found: {fileId}");
        }

        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var expiryUnix = ((DateTimeOffset)expiry).ToUnixTimeSeconds();

        // HMAC-signed token — verified by the download endpoint (see UrlSigningService).
        var token = _urlSigning.Sign(fileId, expiryUnix);

        return $"/api/files/{fileId}/download?token={token}";
    }

    private async Task<string?> FindFilePathAsync(string fileId)
    {
        // Search through date-based directory structure
        var basePath = _options.BasePath;
        
        if (!Directory.Exists(basePath))
        {
            return null;
        }
        
        // Search recursively for the file
        var pattern = $"{fileId}.*";
        var files = Directory.GetFiles(basePath, pattern, SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta.json"))
            .ToArray();
        
        return files.Length > 0 ? files[0] : null;
    }

    private async Task<string?> FindMetadataPathAsync(string fileId)
    {
        var basePath = _options.BasePath;
        
        if (!Directory.Exists(basePath))
        {
            return null;
        }
        
        var pattern = $"{fileId}.meta.json";
        var files = Directory.GetFiles(basePath, pattern, SearchOption.AllDirectories);
        
        return files.Length > 0 ? files[0] : null;
    }
}
