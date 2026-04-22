namespace mediconyx_api.Models.DTOs;

public class FileAttachmentDto
{
    public int Id { get; set; }
    public string StorageFileId { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
}

public class FileAttachmentListResponse
{
    public List<FileAttachmentDto> Attachments { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class FileAttachmentQueryParams
{
    public int? PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public FileAttachmentCategory? Category { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "UploadedAt";
    public string SortOrder { get; set; } = "desc";
}

public class FileUploadRequest
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public FileAttachmentCategory Category { get; set; }
    public string? Description { get; set; }
}
