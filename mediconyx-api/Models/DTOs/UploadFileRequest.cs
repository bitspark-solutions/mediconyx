using Microsoft.AspNetCore.Http;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

/// <summary>
/// Multipart form payload for POST /api/file-attachment/upload.
/// Wrapped in a DTO so Swashbuckle 10 can introspect the schema
/// (the previous pattern of [FromForm] per-parameter with IFormFile
///  threw SwaggerGeneratorException at parameter-discovery time).
/// Recipe: Swashbuckle docs § "Handle Forms and File Uploads".
/// </summary>
public class UploadFileRequest
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public FileAttachmentCategory Category { get; set; }
    public string? Description { get; set; }
    public IFormFile File { get; set; } = null!;
}