using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Patients;

public interface IPatientService
{
    Task<PatientListResponse> GetPatientsAsync(int tenantId, PatientQueryParams queryParams);
    Task<PatientDto?> GetPatientByIdAsync(int tenantId, int patientId);
    Task<PatientDto> CreatePatientAsync(int tenantId, int createdByUserId, CreatePatientRequest request);
    Task<PatientDto> UpdatePatientAsync(int tenantId, int patientId, UpdatePatientRequest request);
    Task DeactivatePatientAsync(int tenantId, int patientId);
}
