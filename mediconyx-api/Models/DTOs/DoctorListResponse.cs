namespace mediconyx_api.Models.DTOs;

public class DoctorListResponse
{
    public List<DoctorProfileDto> Doctors { get; set; } = new();
    public int TotalCount { get; set; }
}
