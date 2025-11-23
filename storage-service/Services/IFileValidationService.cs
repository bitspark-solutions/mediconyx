namespace storage_service.Services;

public interface IFileValidationService
{
    Task<(bool IsValid, string? ErrorMessage)> ValidateFileAsync(
        Stream fileStream, 
        string fileName, 
        string mimeType, 
        long fileSize);
    
    bool IsAllowedMimeType(string mimeType);
    bool IsFileSizeAllowed(long fileSize);
}
