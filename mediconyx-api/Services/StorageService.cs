using System.Net.Http.Json;
using mediconyx_api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace mediconyx_api.Services;

public class StorageServiceOptions
{
    public string BaseUrl { get; set; } = "http://localhost:7890";
}

public class StorageService : IStorageService
{
    private readonly HttpClient _httpClient;
    private readonly StorageServiceOptions _options;
    private readonly ILogger<StorageService> _logger;

    public StorageService(
        HttpClient httpClient,
        IOptions<StorageServiceOptions> options,
        ILogger<StorageService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<StorageUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/api/files/upload", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Storage upload failed: {StatusCode} - {Error}", response.StatusCode, error);
                return new StorageUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Upload failed: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<StorageUploadResponse>(ct);
            if (result == null)
            {
                return new StorageUploadResult { Success = false, ErrorMessage = "Invalid response from storage service" };
            }

            return new StorageUploadResult
            {
                Success = true,
                StorageFileId = result.FileId,
                FileSize = result.SizeBytes,
                SHA256 = result.SHA256
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to storage");
            return new StorageUploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<Stream?> DownloadAsync(string storageFileId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.BaseUrl}/api/files/{storageFileId}/download",
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("File download failed: {StatusCode} for file {FileId}", response.StatusCode, storageFileId);
                return null;
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", storageFileId);
            return null;
        }
    }

    public async Task<StorageFileMetadata?> GetMetadataAsync(string storageFileId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.BaseUrl}/api/files/{storageFileId}/metadata",
                ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<StorageFileMetadata>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for file {FileId}", storageFileId);
            return null;
        }
    }

    public async Task<string> GenerateDownloadUrlAsync(string storageFileId, int expiryMinutes = 60, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.BaseUrl}/api/files/{storageFileId}/url?expiryMinutes={expiryMinutes}",
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to generate URL: {StatusCode}", response.StatusCode);
                return string.Empty;
            }

            var result = await response.Content.ReadFromJsonAsync<StorageUrlResponse>(ct);
            return result?.Url ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for file {FileId}", storageFileId);
            return string.Empty;
        }
    }

    public async Task<bool> DeleteAsync(string storageFileId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"{_options.BaseUrl}/api/files/{storageFileId}",
                ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", storageFileId);
            return false;
        }
    }

    private class StorageUploadResponse
    {
        public string FileId { get; set; } = string.Empty;
        public string SHA256 { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    private class StorageUrlResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
