namespace mediconyx_api.Models.DTOs;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public string? Designation { get; set; }
    public int? PrimaryDepartmentId { get; set; }
    public UserRole[]? AdditionalRoles { get; set; }
    public int TenantId { get; set; }
}
