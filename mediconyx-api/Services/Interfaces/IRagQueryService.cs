using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Interfaces;

public interface IRagQueryService
{
    /// <summary>
    /// Query patient context using RAG and return AI response with citations
    /// </summary>
    Task<RagQueryResponse> QueryPatientContextAsync(
        int patientId,
        string query,
        int tenantId,
        int? conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a patient health summary for doctors
    /// </summary>
    Task<PatientHealthSummary> GetPatientSummaryAsync(
        int patientId,
        int tenantId,
        SummaryDepth depth = SummaryDepth.Standard,
        CancellationToken ct = default);

    /// <summary>
    /// Get or create a conversation for a patient
    /// </summary>
    Task<AIConversationDto> GetOrCreateConversationAsync(
        int patientId,
        int userId,
        int tenantId,
        string? title = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get conversation history
    /// </summary>
    Task<List<AIMessageDto>> GetConversationMessagesAsync(
        int conversationId,
        int tenantId,
        CancellationToken ct = default);
}

public enum SummaryDepth
{
    Brief = 0,
    Standard = 1,
    Comprehensive = 2
}
