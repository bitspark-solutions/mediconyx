namespace mediconyx_api.Models.DTOs;

public class BranchDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsMain { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
