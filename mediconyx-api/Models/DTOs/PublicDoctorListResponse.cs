namespace mediconyx_api.Models.DTOs;

public class PublicDoctorListResponse
{
    public int TotalCount { get; set; }
    public List<PublicDoctorDto> Doctors { get; set; } = new();
    public List<string> AvailableSpecializations { get; set; } = new();
}
