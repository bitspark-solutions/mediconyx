namespace mediconyx_api.Models;

public class Department
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The doctor who heads this department. Nullable — department may not have a
    /// formal head yet, or the head may be a non-doctor (e.g. admin director).
    /// </summary>
    public int? HeadDoctorId { get; set; }
    public DoctorProfile? HeadDoctor { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
