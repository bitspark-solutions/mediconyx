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

public static class ServiceTypes
{
    public const string Consultation = "Consultation";
    public const string Therapy = "Therapy";
    public const string Surgery = "Surgery";
    public const string Diagnosis = "Diagnosis";
    public const string Lab = "Lab";
    public const string Radiology = "Radiology";
    public const string Pharmacy = "Pharmacy";
    public const string Emergency = "Emergency";
    public const string ICU = "ICU";
    public const string PICU = "PICU";
    public const string NICU = "NICU";
    public const string DayCare = "DayCare";
    public const string Maternity = "Maternity";
    public const string Pediatric = "Pediatric";
    public const string Geriatric = "Geriatric";
    public const string General = "General";
    public const string Other = "Other";

    public static readonly string[] All = {
        Consultation, Therapy, Surgery, Diagnosis, Lab, Radiology, Pharmacy, Emergency,
        ICU, PICU, NICU, DayCare, Maternity, Pediatric, Geriatric, General, Other
    };

    /// <summary>Service types that are wards (have beds). The rest are OPD services.</summary>
    public static readonly string[] WardTypes = {
        General, ICU, PICU, NICU, DayCare, Maternity, Pediatric, Geriatric, Surgery
    };
}