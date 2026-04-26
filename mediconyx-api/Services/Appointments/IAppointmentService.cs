using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Appointments;

public interface IAppointmentService
{
    Task<AppointmentListResponse> GetAppointmentsAsync(int tenantId, AppointmentQueryParams queryParams);
    Task<AppointmentDto?> GetAppointmentByIdAsync(int tenantId, int appointmentId);
    Task<AppointmentDto> CreateAppointmentAsync(int tenantId, int createdByUserId, CreateAppointmentRequest request);
    Task<AppointmentDto> UpdateAppointmentAsync(int tenantId, int appointmentId, UpdateAppointmentRequest request);
    Task<AppointmentDto> UpdateStatusAsync(int tenantId, int appointmentId, AppointmentStatus status);
    Task<AppointmentDto> CancelAppointmentAsync(int tenantId, int appointmentId, string reason);
    Task DeleteAppointmentAsync(int tenantId, int appointmentId, int adminUserId);
    Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(int tenantId, AvailableSlotsQueryParams queryParams);
}
