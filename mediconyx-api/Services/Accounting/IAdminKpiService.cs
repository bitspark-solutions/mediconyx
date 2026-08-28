using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Accounting;

public interface IAdminKpiService
{
    Task<AdminFinancialKpiDto> GetFinancialKpisAsync(int tenantId, AdminKpiQueryParams query);
    Task<AdminClinicalKpiDto> GetClinicalKpisAsync(int tenantId, AdminKpiQueryParams query);
    Task<List<DepartmentalRevenueDto>> GetDepartmentalRevenueAsync(int tenantId, AdminKpiQueryParams query);
    Task<List<RevenueTrendDto>> GetRevenueTrendAsync(int tenantId, AdminKpiQueryParams query);
}
