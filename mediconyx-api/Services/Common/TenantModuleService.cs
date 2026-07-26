using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Middleware;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Common;

/// <summary>
/// Enforces tenant module toggles. Until now the 12 Module*Enabled flags were stored
/// but never checked — this is the single enforcement point.
/// </summary>
public class TenantModuleService : ITenantModuleService
{
    private readonly ApplicationDbContext _context;

    public TenantModuleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsEnabledAsync(int tenantId, string moduleKey)
    {
        var flags = await _context.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => new
            {
                t.ModuleOpdEnabled, t.ModuleIpdEnabled, t.ModuleEmergencyEnabled,
                t.ModulePharmacyEnabled, t.ModuleLaboratoryEnabled, t.ModuleRadiologyEnabled,
                t.ModuleBillingEnabled, t.ModuleInventoryEnabled, t.ModuleHrPayrollEnabled,
                t.ModuleAppointmentEnabled, t.ModuleTelemedicineEnabled, t.ModuleAnalyticsEnabled
            })
            .FirstOrDefaultAsync();

        if (flags == null) return false;

        return moduleKey switch
        {
            ModuleKeys.Opd => flags.ModuleOpdEnabled,
            ModuleKeys.Ipd => flags.ModuleIpdEnabled,
            ModuleKeys.Emergency => flags.ModuleEmergencyEnabled,
            ModuleKeys.Pharmacy => flags.ModulePharmacyEnabled,
            ModuleKeys.Laboratory => flags.ModuleLaboratoryEnabled,
            ModuleKeys.Radiology => flags.ModuleRadiologyEnabled,
            ModuleKeys.Billing => flags.ModuleBillingEnabled,
            ModuleKeys.Inventory => flags.ModuleInventoryEnabled,
            ModuleKeys.HrPayroll => flags.ModuleHrPayrollEnabled,
            ModuleKeys.Appointment => flags.ModuleAppointmentEnabled,
            ModuleKeys.Telemedicine => flags.ModuleTelemedicineEnabled,
            ModuleKeys.Analytics => flags.ModuleAnalyticsEnabled,
            _ => false
        };
    }

    public async Task RequireEnabledAsync(int tenantId, string moduleKey, string displayName)
    {
        if (!await IsEnabledAsync(tenantId, moduleKey))
            throw new ModuleDisabledException($"The {displayName} module is not enabled for this hospital. Contact the platform administrator.");
    }
}
