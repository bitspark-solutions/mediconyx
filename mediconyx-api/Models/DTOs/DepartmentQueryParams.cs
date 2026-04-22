namespace mediconyx_api.Models.DTOs;

public class DepartmentQueryParams
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50; // Usually small number of departments
}
