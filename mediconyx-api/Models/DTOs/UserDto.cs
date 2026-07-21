namespace mediconyx_api.Models.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public int? PrimaryDepartmentId { get; set; }
    public List<string> AdditionalRoles { get; set; } = new();
    public int TenantId { get; set; }
}
