namespace mediconyx_api.Models.DTOs;

public class UserDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public int? PrimaryDepartmentId { get; set; }
    public string? PrimaryDepartmentName { get; set; }
    public int? PrimaryBranchId { get; set; }
    public string? PrimaryBranchName { get; set; }
    public List<string> AdditionalRoles { get; set; } = new();
    public bool IsActive { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
