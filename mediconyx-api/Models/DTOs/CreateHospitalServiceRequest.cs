namespace mediconyx_api.Models.DTOs;

public class CreateHospitalServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public decimal? BasePrice { get; set; }
    public int? DepartmentId { get; set; }
}
