using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public class TechnicalRequirements
{
    public int Id { get; set; }
    
    // Link to SalesRequest (1:1 relationship)
    public int SalesRequestId { get; set; }
    public SalesRequest SalesRequest { get; set; } = null!;
    
    // User and Infrastructure Requirements
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
    public string DepartmentsList { get; set; } = string.Empty; // Comma-separated
    
    // Existing Systems
    [StringLength(200)]
    public string? ExistingEmrSystem { get; set; }
    
    [StringLength(2000)]
    public string? IntegrationRequirements { get; set; }
    
    [Required]
    public bool DataMigrationRequired { get; set; }
    
    [Range(0, 10000000)]
    public int? EstimatedPatientRecords { get; set; }
    
    public DateTime? PreferredGoLiveDate { get; set; }
    
    // IT Contact Information
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
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
