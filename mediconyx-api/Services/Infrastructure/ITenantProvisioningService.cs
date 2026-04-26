namespace mediconyx_api.Services.Infrastructure;

public interface ITenantProvisioningService
{
    Task<ProvisioningResult> ProvisionTenantAsync(int salesRequestId);
}
