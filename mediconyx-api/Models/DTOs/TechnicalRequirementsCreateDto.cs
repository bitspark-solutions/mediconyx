using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models.DTOs;

public class TechnicalRequirementsCreateDto
{
    [Required]
    [Range(1, 10000)]
    public int ExpectedUserCount { get; set; }
    
    [Required]
    [Range(1, 1000)]
    public int ConcurrentUsers { get; set; }
    
    [Required]
    [Range(1, 100)]
    public int DepartmentCount { get; set; }
    
    [Required]
    [StringLength(2000)]
    public string DepartmentsList { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? ExistingEmrSystem { get; set; }
    
    [StringLength(2000)]
    public string? IntegrationRequirements { get; set; }
    
    [Required]
    public bool DataMigrationRequired { get; set; }
    
    [Range(0, 10000000)]
    public int? EstimatedPatientRecords { get; set; }
    
    public DateTime? PreferredGoLiveDate { get; set; }
    
    [Required]
    [StringLength(200)]
    public string ItContactName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string ItContactEmail { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string ItContactPhone { get; set; } = string.Empty;
}
