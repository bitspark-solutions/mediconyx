namespace mediconyx_api.Models.DTOs;

public class PatientListResponse
{
    public List<PatientDto> Patients { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
