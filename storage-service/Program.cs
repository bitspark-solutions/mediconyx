using storage_service.Configuration;
using storage_service.Interfaces;
using storage_service.Providers;
using storage_service.Services;
using storage_service.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<UrlSigningService>();
builder.Services.AddSingleton<IStorageProvider, LocalFileSystemProvider>();
builder.Services.AddSingleton<IFileValidationService, FileValidationService>();
builder.Services.AddHostedService<SoftDeletePurgeService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMediconyxApi", policy =>
    {
        policy.WithOrigins("http://api:8080", "http://localhost:9765")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

var storageOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
if (string.IsNullOrWhiteSpace(storageOptions.ApiKey))
{
    app.Logger.LogWarning("Storage:ApiKey is not set — API-key auth is DISABLED. Set it in any deployed environment.");
}

app.UseCors("AllowMediconyxApi");

// API-key auth for all /api/* endpoints (server-to-server with the main API).
// Exceptions: /health, and downloads carrying a signed token (verified by the endpoint itself).
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }
    if (string.IsNullOrWhiteSpace(storageOptions.ApiKey))
    {
        await next(); // dev mode — auth disabled
        return;
    }
    if (context.Request.Headers["X-Api-Key"] == storageOptions.ApiKey)
    {
        await next();
        return;
    }
    if (context.Request.Path.Value?.EndsWith("/download") == true && context.Request.Query.ContainsKey("token"))
    {
        await next(); // signed token is verified by the download endpoint
        return;
    }
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    await context.Response.WriteAsJsonAsync(new { message = "Missing or invalid API key" });
});

// Health check endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        service = "storage-service"
    });
});

// Upload file endpoint
app.MapPost("/api/files/upload", async (
    IFormFile file,
    IStorageProvider storageProvider,
    IFileValidationService validationService,
    ILogger<Program> logger) =>
{
    try
    {
        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "No file uploaded" });
        }

        logger.LogInformation("Upload request: {FileName}, Size: {Size} bytes, ContentType: {ContentType}",
            file.FileName, file.Length, file.ContentType);

        // Validate file
        using var stream = file.OpenReadStream();
        var validation = await validationService.ValidateFileAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        if (!validation.IsValid)
        {
            logger.LogWarning("File validation failed: {Error}", validation.ErrorMessage);
            return Results.BadRequest(new { message = validation.ErrorMessage });
        }

        // Reset stream for upload
        stream.Position = 0;

        // Upload file
        var result = await storageProvider.UploadAsync(stream, file.FileName, file.ContentType);

        if (!result.Success)
        {
            logger.LogError("Upload failed: {Error}", result.ErrorMessage);
            return Results.StatusCode(500);
        }

        return Results.Created($"/api/files/{result.FileId}", new UploadResult
        {
            FileId = result.FileId!,
            SHA256 = result.Metadata!.SHA256,
            UploadedAt = result.Metadata.UploadedAt,
            SizeBytes = result.Metadata.SizeBytes
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling file upload");
        return Results.StatusCode(500);
    }
}).DisableAntiforgery();

// Download file endpoint
app.MapGet("/api/files/{fileId}/download", async (
    string fileId,
    string? token,
    IStorageProvider storageProvider,
    UrlSigningService urlSigning,
    ILogger<Program> logger) =>
{
    try
    {
        // A token query param must be a valid HMAC-signed URL token.
        // (Requests without a token arrive here only with a valid X-Api-Key, or in dev mode.)
        if (token != null && !urlSigning.Verify(fileId, token))
        {
            logger.LogWarning("Rejected download with invalid/expired token: {FileId}", fileId);
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var metadata = await storageProvider.GetMetadataAsync(fileId);
        
        if (metadata == null)
        {
            return Results.NotFound(new { message = "File not found" });
        }

        if (metadata.IsDeleted)
        {
            return Results.NotFound(new { message = "File has been deleted" });
        }

        var stream = await storageProvider.DownloadAsync(fileId);
        
        if (stream == null)
        {
            return Results.NotFound(new { message = "File not found" });
        }

        logger.LogInformation("File download: {FileId}, FileName: {FileName}", fileId, metadata.FileName);
        
        return Results.File(stream, metadata.MimeType, metadata.FileName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error downloading file {FileId}", fileId);
        return Results.StatusCode(500);
    }
});

// Get file metadata endpoint
app.MapGet("/api/files/{fileId}/metadata", async (
    string fileId,
    IStorageProvider storageProvider) =>
{
    var metadata = await storageProvider.GetMetadataAsync(fileId);
    
    if (metadata == null)
    {
        return Results.NotFound(new { message = "File not found" });
    }

    return Results.Ok(metadata);
});

// Generate file URL endpoint
app.MapGet("/api/files/{fileId}/url", async (
    string fileId,
    int expiryMinutes,
    IStorageProvider storageProvider,
    ILogger<Program> logger) =>
{
    try
    {
        if (expiryMinutes <= 0)
        {
            expiryMinutes = 60; // Default 60 minutes
        }

        var url = await storageProvider.GenerateUrlAsync(fileId, expiryMinutes);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        return Results.Ok(new UrlResult
        {
            Url = url,
            ExpiresAt = expiresAt
        });
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound(new { message = "File not found" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating URL for file {FileId}", fileId);
        return Results.StatusCode(500);
    }
});

// Delete file endpoint
app.MapDelete("/api/files/{fileId}", async (
    string fileId,
    IStorageProvider storageProvider,
    ILogger<Program> logger) =>
{
    try
    {
        var deleted = await storageProvider.DeleteAsync(fileId);
        
        if (!deleted)
        {
            return Results.NotFound(new { message = "File not found" });
        }

        logger.LogInformation("File deleted: {FileId}", fileId);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting file {FileId}", fileId);
        return Results.StatusCode(500);
    }
});

app.Logger.LogInformation("Storage service starting...");
app.Run();

partial class Program { }
