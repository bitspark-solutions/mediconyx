namespace mediconyx_api.Models.DTOs;

public class UpdateTenantSettingsRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Description { get; set; }
    public string? OperatingHours { get; set; }
    public string? TimeZone { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
}