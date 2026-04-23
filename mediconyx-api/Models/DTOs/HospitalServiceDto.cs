namespace mediconyx_api.Models.DTOs;

public class HospitalServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal? BasePrice { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
}

public class CreateHospitalServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public decimal? BasePrice { get; set; }
    public int? DepartmentId { get; set; }
}

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