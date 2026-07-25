using Microsoft.Extensions.Options;
using storage_service.Configuration;

namespace storage_service.Services;

public class FileValidationService : IFileValidationService
{
    private readonly StorageOptions _options;
    private readonly ILogger<FileValidationService> _logger;

    // Magic numbers for file type detection
    private static readonly Dictionary<string, byte[][]> MagicNumbers = new()
    {
        { "application/pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }, // %PDF
        { "image/jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { "image/png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { "image/gif", new[] {
            new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
            new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }  // GIF89a
        } },
        { 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // DOCX
            new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } // ZIP signature (DOCX is ZIP)
        },
        { 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // XLSX
            new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } // ZIP signature
        },
        { 
            "application/msword", // DOC
            new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } // OLE2
        },
        { 
            "application/vnd.ms-excel", // XLS
            new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } // OLE2
        }
    };

    public FileValidationService(
        IOptions<StorageOptions> options,
        ILogger<FileValidationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateFileAsync(
        Stream fileStream, 
        string fileName, 
        string mimeType, 
        long fileSize)
    {
        // Validate MIME type
        if (!IsAllowedMimeType(mimeType))
        {
            return (false, $"File type '{mimeType}' is not allowed. Allowed types: {string.Join(", ", _options.AllowedMimeTypes)}");
        }

        // Validate file size
        if (!IsFileSizeAllowed(fileSize))
        {
            var maxSizeMB = _options.MaxFileSizeBytes / (1024 * 1024);
            return (false, $"File size exceeds maximum allowed size of {maxSizeMB}MB");
        }

        // Validate magic number
        var magicNumberValid = await ValidateMagicNumberAsync(fileStream, mimeType);
        if (!magicNumberValid)
        {
            return (false, "File content does not match declared file type (magic number validation failed)");
        }

        // Reset stream position after reading magic number
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        // Additional validation: virus scanning stub
        // In production, integrate with ClamAV or similar
        var virusScanResult = await ScanForVirusesAsync(fileStream);
        if (!virusScanResult)
        {
            return (false, "File failed virus scan");
        }

        return (true, null);
    }

    public bool IsAllowedMimeType(string mimeType)
    {
        return _options.AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsFileSizeAllowed(long fileSize)
    {
        return fileSize > 0 && fileSize <= _options.MaxFileSizeBytes;
    }

    private async Task<bool> ValidateMagicNumberAsync(Stream fileStream, string mimeType)
    {
        try
        {
            // DICOM stores its "DICM" marker at byte offset 128, after a 128-byte preamble.
            if (mimeType.Equals("application/dicom", StringComparison.OrdinalIgnoreCase))
            {
                var dicomBuffer = new byte[132];
                var dicomRead = await fileStream.ReadAsync(dicomBuffer, 0, dicomBuffer.Length);
                var isDicom = dicomRead == dicomBuffer.Length
                    && dicomBuffer[128] == (byte)'D' && dicomBuffer[129] == (byte)'I'
                    && dicomBuffer[130] == (byte)'C' && dicomBuffer[131] == (byte)'M';
                if (!isDicom)
                    _logger.LogWarning("Magic number validation failed for application/dicom (no DICM preamble marker)");
                return isDicom;
            }

            if (!MagicNumbers.ContainsKey(mimeType))
            {
                // No magic number defined for this type, skip validation
                _logger.LogWarning("No magic number validation available for MIME type: {MimeType}", mimeType);
                return true;
            }

            var expectedSignatures = MagicNumbers[mimeType];
            var maxSignatureLength = expectedSignatures.Max(s => s.Length);
            
            var buffer = new byte[maxSignatureLength];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, maxSignatureLength);

            if (bytesRead == 0)
            {
                _logger.LogWarning("Empty file stream during magic number validation");
                return false;
            }

            // Check if any of the expected signatures match
            foreach (var signature in expectedSignatures)
            {
                if (bytesRead >= signature.Length)
                {
                    var matches = true;
                    for (int i = 0; i < signature.Length; i++)
                    {
                        if (buffer[i] != signature[i])
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        _logger.LogDebug("Magic number validation passed for {MimeType}", mimeType);
                        return true;
                    }
                }
            }

            _logger.LogWarning(
                "Magic number validation failed for {MimeType}. Expected one of {Count} signatures, got: {Actual}",
                mimeType,
                expectedSignatures.Length,
                BitConverter.ToString(buffer.Take(Math.Min(8, bytesRead)).ToArray()));
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during magic number validation");
            return false;
        }
    }

    private async Task<bool> ScanForVirusesAsync(Stream fileStream)
    {
        // Virus scanning stub
        // In production environment, integrate with ClamAV or similar:
        // - Send file to ClamAV daemon via TCP socket
        // - Wait for scan result
        // - Return true if clean, false if virus detected
        
        _logger.LogDebug("Virus scan: STUB (always passes in development)");
        
        // For now, always return true
        await Task.CompletedTask;
        return true;
    }
}
