namespace mediconyx_api.Models.DTOs;

public class PrescriptionListResponse
{
    public List<PrescriptionDto> Prescriptions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
