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

    // Module toggles (null = unchanged)
    public bool? ModuleOpdEnabled { get; set; }
    public bool? ModuleIpdEnabled { get; set; }
    public bool? ModuleEmergencyEnabled { get; set; }
    public bool? ModulePharmacyEnabled { get; set; }
    public bool? ModuleLaboratoryEnabled { get; set; }
    public bool? ModuleRadiologyEnabled { get; set; }
    public bool? ModuleBillingEnabled { get; set; }
    public bool? ModuleInventoryEnabled { get; set; }
    public bool? ModuleHrPayrollEnabled { get; set; }
    public bool? ModuleAppointmentEnabled { get; set; }
    public bool? ModuleTelemedicineEnabled { get; set; }
    public bool? ModuleAnalyticsEnabled { get; set; }
}