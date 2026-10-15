namespace mediconyx_api.Models.DTOs;

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
