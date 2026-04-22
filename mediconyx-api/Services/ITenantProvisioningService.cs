namespace mediconyx_api.Services;

public interface ITenantProvisioningService
{
    Task<ProvisioningResult> ProvisionTenantAsync(int salesRequestId);
}


