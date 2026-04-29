using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public class HospitalProfile
{
    public int Id { get; set; }
    
    // Link to SalesRequest (1:1 relationship)
    public int SalesRequestId { get; set; }
    public SalesRequest SalesRequest { get; set; } = null!;
    
    // Legal Information
    [Required]
    [StringLength(500)]
    public string HospitalLegalName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LicenseNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string TaxId { get; set; } = string.Empty;
    
    [Required]
    public HospitalType HospitalType { get; set; }
    
    [Required]
    [Range(1, 10000)]
    public int BedCapacity { get; set; }
    
    // Address Information
    [Required]
    [StringLength(500)]
    public string AddressLine1 { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? AddressLine2 { get; set; }
    
    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string StateProvince { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;
    
    // Primary Contact (inherited from SalesRequest but can be updated)
    [Required]
    [StringLength(200)]
    public string PrimaryContactName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string PrimaryContactEmail { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string PrimaryContactPhone { get; set; } = string.Empty;
    
    // Secondary Contact (optional)
    [StringLength(200)]
    public string? SecondaryContactName { get; set; }
    
    [EmailAddress]
    [StringLength(200)]
    public string? SecondaryContactEmail { get; set; }
    
    [StringLength(50)]
    public string? SecondaryContactPhone { get; set; }
    
    // Additional Information
    [Url]
    [StringLength(500)]
    public string? WebsiteUrl { get; set; }
    
    [Range(1800, 2100)]
    public int? EstablishedYear { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}


