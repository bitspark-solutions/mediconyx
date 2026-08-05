namespace mediconyx_api.Models.DTOs;

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public UserRole? Role { get; set; }
    public string? Designation { get; set; }
    public int? PrimaryDepartmentId { get; set; }
    public int? PrimaryBranchId { get; set; }
    public UserRole[]? AdditionalRoles { get; set; }
    public bool? IsActive { get; set; }
}
