namespace mediconyx_api.Services.Infrastructure;

public interface IFileValidationService
{
    Task<(bool IsValid, string? ErrorMessage)> ValidateUploadAsync(IFormFile file);
    bool IsAllowedFileType(string fileName, string contentType);
}
