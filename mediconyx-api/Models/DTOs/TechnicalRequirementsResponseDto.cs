namespace mediconyx_api.Models.DTOs;

public class TechnicalRequirementsResponseDto
{
    public int Id { get; set; }
    public int SalesRequestId { get; set; }
    public int ExpectedUserCount { get; set; }
    public int ConcurrentUsers { get; set; }
    public int DepartmentCount { get; set; }
    public string DepartmentsList { get; set; } = string.Empty;
    public string? ExistingEmrSystem { get; set; }
    public string? IntegrationRequirements { get; set; }
    public bool DataMigrationRequired { get; set; }
    public int? EstimatedPatientRecords { get; set; }
    public DateTime? PreferredGoLiveDate { get; set; }
    public string ItContactName { get; set; } = string.Empty;
    public string ItContactEmail { get; set; } = string.Empty;
    public string ItContactPhone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
