namespace mediconyx_api.Models.DTOs;

public class AppointmentQueryParams
{
    public int? PatientId { get; set; }
    public int? DoctorId { get; set; }
    public int? DepartmentId { get; set; }
    public int? BranchId { get; set; }
    public AppointmentStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "AppointmentDate";
    public string? SortOrder { get; set; } = "asc";
}
