namespace mediconyx_api.Models.DTOs;

public class PatientQueryParams
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public Gender? Gender { get; set; }
    public string? BloodGroup { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortOrder { get; set; } = "desc";
}
