using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models.DTOs;

public class HospitalProfileCreateDto
{
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
    
    [StringLength(200)]
    public string? SecondaryContactName { get; set; }
    
    [EmailAddress]
    [StringLength(200)]
    public string? SecondaryContactEmail { get; set; }
    
    [StringLength(50)]
    public string? SecondaryContactPhone { get; set; }
    
    [Url]
    [StringLength(500)]
    public string? WebsiteUrl { get; set; }
    
    [Range(1800, 2100)]
    public int? EstablishedYear { get; set; }
}

public class HospitalProfileResponseDto
{
    public int Id { get; set; }
    public int SalesRequestId { get; set; }
    public string HospitalLegalName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public HospitalType HospitalType { get; set; }
    public int BedCapacity { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PrimaryContactName { get; set; } = string.Empty;
    public string PrimaryContactEmail { get; set; } = string.Empty;
    public string PrimaryContactPhone { get; set; } = string.Empty;
    public string? SecondaryContactName { get; set; }
    public string? SecondaryContactEmail { get; set; }
    public string? SecondaryContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public int? EstablishedYear { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
