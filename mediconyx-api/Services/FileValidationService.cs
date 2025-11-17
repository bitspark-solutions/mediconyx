namespace mediconyx_api.Services;

public class FileValidationService : IFileValidationService
{
    private readonly ILogger<FileValidationService> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    private static readonly Dictionary<string, string[]> AllowedFileTypes = new()
    {
        { ".pdf", new[] { "application/pdf" } },
        { ".doc", new[] { "application/msword" } },
        { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
        { ".png", new[] { "image/png" } },
        { ".jpg", new[] { "image/jpeg" } },
        { ".jpeg", new[] { "image/jpeg" } }
    };

    // Known malicious file signatures (magic bytes)
    private static readonly Dictionary<string, byte[]> MaliciousSignatures = new()
    {
        { "EXE", new byte[] { 0x4D, 0x5A } }, // MZ header (Windows executable)
        { "ELF", new byte[] { 0x7F, 0x45, 0x4C, 0x46 } }, // ELF header (Linux executable)
        { "SCRIPT", new byte[] { 0x3C, 0x3F, 0x70, 0x68, 0x70 } } // <?php
    };

    public FileValidationService(ILogger<FileValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateUploadAsync(IFormFile file)
    {
        // Check if file exists
        if (file == null || file.Length == 0)
        {
            return (false, "No file provided");
        }

        // Check file size
        if (file.Length > MaxFileSize)
        {
            return (false, $"File size exceeds {MaxFileSize / 1024 / 1024}MB limit");
        }

        // Check file extension and content type
        if (!IsAllowedFileType(file.FileName, file.ContentType))
        {
            return (false, $"Invalid file type. Allowed: {string.Join(", ", AllowedFileTypes.Keys)}");
        }

        // Validate file signature (magic bytes)
        var signatureCheck = await ValidateFileSignatureAsync(file);
        if (!signatureCheck.IsValid)
        {
            return (false, signatureCheck.ErrorMessage);
        }

        return (true, null);
    }

    public bool IsAllowedFileType(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        if (!AllowedFileTypes.TryGetValue(extension, out var allowedMimeTypes))
        {
            return false;
        }

        return allowedMimeTypes.Contains(contentType);
    }

    private async Task<(bool IsValid, string? ErrorMessage)> ValidateFileSignatureAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[8]; // Read first 8 bytes for signature check
            var bytesRead = await stream.ReadAsync(buffer);
            
            if (bytesRead < 4)
            {
                return (false, "File is too small or corrupted");
            }

            // Check for malicious signatures
            foreach (var (type, signature) in MaliciousSignatures)
            {
                if (StartsWithSignature(buffer, signature))
                {
                    _logger.LogWarning("Malicious file detected: {Type}, FileName: {FileName}", type, file.FileName);
                    return (false, "File contains potentially malicious content");
                }
            }

            // Validate expected signatures for allowed file types
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
        if (buffer.Length < signature.Length)
            return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i])
                return false;
        }

        return true;
    }

    private static byte[]? GetExpectedSignature(string extension)
    {
        return extension switch
        {
            ".pdf" => new byte[] { 0x25, 0x50, 0x44, 0x46 }, // %PDF
            ".png" => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, // PNG signature
            ".jpg" or ".jpeg" => new byte[] { 0xFF, 0xD8, 0xFF }, // JPEG signature
            _ => null // Word documents have complex signatures, skip for now
        };
    }
}
