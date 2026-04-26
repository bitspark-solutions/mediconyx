namespace mediconyx_api.Services.Infrastructure;

public class ProvisioningResult
{
    public bool Success { get; set; }
    public int? TenantId { get; set; }
    public int? AdminUserId { get; set; }
    public string? Subdomain { get; set; }
    public string? AdminEmail { get; set; }
    public string? TemporaryPassword { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
}
