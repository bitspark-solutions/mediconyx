using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.AI;

public interface IRagQueryService
{
    Task<RagQueryResponse> QueryPatientContextAsync(
        int patientId,
        string query,
        int tenantId,
        int? conversationId,
        CancellationToken ct = default);

    Task<PatientHealthSummary> GetPatientSummaryAsync(
        int patientId,
        int tenantId,
        SummaryDepth depth = SummaryDepth.Standard,
        CancellationToken ct = default);

    Task<AIConversationDto> GetOrCreateConversationAsync(
        int patientId,
        int userId,
        int tenantId,
        string? title = null,
        CancellationToken ct = default);

    Task<List<AIMessageDto>> GetConversationMessagesAsync(
        int conversationId,
        int tenantId,
        CancellationToken ct = default);
}
