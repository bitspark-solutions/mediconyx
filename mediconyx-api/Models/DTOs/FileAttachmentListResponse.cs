namespace mediconyx_api.Models.DTOs;

public class FileAttachmentListResponse
{
    public List<FileAttachmentDto> Attachments { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
