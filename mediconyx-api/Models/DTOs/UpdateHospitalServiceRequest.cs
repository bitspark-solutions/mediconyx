namespace mediconyx_api.Models.DTOs;

public class UpdateHospitalServiceRequest
{
    public string? Name { get; set; }
    public string? ServiceType { get; set; }
    public string? Description { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal? BasePrice { get; set; }
    public int? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}
