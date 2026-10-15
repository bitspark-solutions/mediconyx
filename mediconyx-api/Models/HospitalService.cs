using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public class HospitalService
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string ServiceType { get; set; } = string.Empty;  // Consultation, Therapy, Surgery, Diagnosis, Lab, Radiology

    [StringLength(500)]
    public string? Description { get; set; }

    public int DurationMinutes { get; set; } = 30;  // Default 30 min

    public decimal? BasePrice { get; set; }  // Optional pricing

    public int? DepartmentId { get; set; }  // Link to department
    public Department? Department { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
