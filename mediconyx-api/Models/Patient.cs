namespace mediconyx_api.Models;

public class Patient
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // Personal Info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? BloodGroup { get; set; }

    // Contact
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NationalId { get; set; }

    // Address (flat fields)
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    // Emergency Contact (flat fields)
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // Internal
    public string PatientNumber { get; set; } = string.Empty; // auto-generated: "P-000001"
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2
}
