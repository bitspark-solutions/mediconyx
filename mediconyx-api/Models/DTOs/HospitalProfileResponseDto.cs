using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

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
