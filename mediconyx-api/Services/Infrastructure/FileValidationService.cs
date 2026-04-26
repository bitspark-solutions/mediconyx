namespace mediconyx_api.Services.Infrastructure;

public class FileValidationService : IFileValidationService
{
    private readonly ILogger<FileValidationService> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    private static readonly Dictionary<string, string[]> AllowedFileTypes = new()
    {
        { ".pdf",  new[] { "application/pdf" } },
        { ".doc",  new[] { "application/msword" } },
        { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
        { ".png",  new[] { "image/png" } },
        { ".jpg",  new[] { "image/jpeg" } },
        { ".jpeg", new[] { "image/jpeg" } }
    };

    private static readonly Dictionary<string, byte[]> MaliciousSignatures = new()
    {
        { "EXE",    new byte[] { 0x4D, 0x5A } },
        { "ELF",    new byte[] { 0x7F, 0x45, 0x4C, 0x46 } },
        { "SCRIPT", new byte[] { 0x3C, 0x3F, 0x70, 0x68, 0x70 } }
    };

    public FileValidationService(ILogger<FileValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateUploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "No file provided");

        if (file.Length > MaxFileSize)
            return (false, $"File size exceeds {MaxFileSize / 1024 / 1024}MB limit");

        if (!IsAllowedFileType(file.FileName, file.ContentType))
            return (false, $"Invalid file type. Allowed: {string.Join(", ", AllowedFileTypes.Keys)}");

        return await ValidateFileSignatureAsync(file);
    }

    public bool IsAllowedFileType(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedFileTypes.TryGetValue(extension, out var allowedMimeTypes)
               && allowedMimeTypes.Contains(contentType);
    }

    private async Task<(bool IsValid, string? ErrorMessage)> ValidateFileSignatureAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[8];
            var bytesRead = await stream.ReadAsync(buffer);

            if (bytesRead < 4)
                return (false, "File is too small or corrupted");

            foreach (var (type, signature) in MaliciousSignatures)
            {
                if (StartsWithSignature(buffer, signature))
                {
                    _logger.LogWarning("Malicious file detected: {Type}, FileName: {FileName}", type, file.FileName);
                    return (false, "File contains potentially malicious content");
                }
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var expectedSignature = GetExpectedSignature(extension);

            if (expectedSignature != null && !StartsWithSignature(buffer, expectedSignature))
            {
                _logger.LogWarning("File signature mismatch for {Extension}, FileName: {FileName}", extension, file.FileName);
                return (false, "File content does not match the file extension");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file signature for {FileName}", file.FileName);
            return (false, "Error validating file");
        }
    }

    private static bool StartsWithSignature(byte[] buffer, byte[] signature)
    {
        if (buffer.Length < signature.Length) return false;
        for (int i = 0; i < signature.Length; i++)
            if (buffer[i] != signature[i]) return false;
        return true;
    }

    private static byte[]? GetExpectedSignature(string extension) => extension switch
    {
        ".pdf"         => new byte[] { 0x25, 0x50, 0x44, 0x46 },
        ".png"         => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
        ".jpg" or ".jpeg" => new byte[] { 0xFF, 0xD8, 0xFF },
        _              => null
    };
}
