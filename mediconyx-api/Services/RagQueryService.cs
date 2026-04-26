using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.AI;

public class RagQueryService : IRagQueryService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILLMService _llmService;
    private readonly ILogger<RagQueryService> _logger;

    private const string PatientSystemPrompt = @"You are a helpful medical assistant.
You are talking to a patient about their personal health records.
You have access to the patient's medical history including:
- Past doctor visits and notes
- prescriptions
- vital signs
- uploaded lab reports

IMPORTANT RULES:
1. Only discuss information that is in the provided context
2. If you're not sure about something, say 'I don't have that information'
3. Always advise the patient to consult their doctor for medical decisions
4. Use plain language, not medical jargon when possible
5. If a question requires urgent attention, advise calling emergency services
6. Never make definitive diagnoses - always recommend professional medical advice
7. Be concise and to the point in your responses";

    private const string DoctorSystemPrompt = @"You are a clinical decision support AI assistant.
You are helping a doctor review a patient's medical history.
You have access to:
- Structured patient data (demographics, conditions)
- Clinical notes from past visits
- Current prescriptions
- Recent vital signs
- Uploaded reports and lab results

Your role:
1. Summarize relevant findings concisely
2. Flag potential drug interactions or concerns
3. Suggest relevant clinical guidelines
4. Identify gaps in information that might help
5. Always cite your sources (which document the information came from)

Format your response with:
- Clinical Summary (2-3 paragraphs)
- Active Problems List
- Current Medications
- Recommendations
- Citations (source documents)";

    public RagQueryService(
        ApplicationDbContext context,
        IEmbeddingService embeddingService,
        ILLMService llmService,
        ILogger<RagQueryService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<RagQueryResponse> QueryPatientContextAsync(
        int patientId,
        string query,
        int tenantId,
        int? conversationId,
        CancellationToken ct = default)
    {
        // Get patient info
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId, ct);
        if (patient == null)
            throw new Exception("Patient not found");

        // Retrieve relevant context from patient data
        var contextDocuments = await RetrievePatientContextAsync(patientId, tenantId, query, ct: ct);

        // Build context string
        var contextString = BuildContextString(contextDocuments);

        // Generate response
        var systemPrompt = PatientSystemPrompt;
        var response = await _llmService.GenerateChatCompletionAsync(
            systemPrompt,
            $"Patient: {patient.FirstName} {patient.LastName}\n\nQuestion: {query}\n\nContext:\n{contextString}",
            conversationHistory: null,
            ct: ct);

        // Save conversation if conversationId provided
        int messageId = 0;
        if (conversationId.HasValue)
        {
            var userMessage = new AIMessage
            {
                ConversationId = conversationId.Value,
                Role = AIMessageRole.User,
                Content = query,
                CreatedAt = DateTime.UtcNow
            };
            _context.AIMessages.Add(userMessage);
            await _context.SaveChangesAsync(ct);

            var assistantMessage = new AIMessage
            {
                ConversationId = conversationId.Value,
                Role = AIMessageRole.Assistant,
                Content = response.Content,
                TokensUsed = response.TokensUsed,
                Confidence = response.Confidence,
                CreatedAt = DateTime.UtcNow
            };
            _context.AIMessages.Add(assistantMessage);
            await _context.SaveChangesAsync(ct);
            messageId = assistantMessage.Id;
        }

        // Build citations from retrieved documents
        var citations = contextDocuments
            .Take(5)
            .Select(d => new RagCitationDto
            {
                SourceType = d.SourceType,
                SourceId = d.SourceId,
                SourceName = d.SourceName,
                Excerpt = d.Content.Length > 200 ? d.Content[..200] + "..." : d.Content,
                RelevanceScore = d.RelevanceScore
            })
            .ToList();

        return new RagQueryResponse
        {
            Answer = response.Content,
            Citations = citations,
            Confidence = response.Confidence,
            SuggestedActions = GenerateSuggestedActions(query, contextDocuments),
            ConversationId = conversationId ?? 0,
            MessageId = messageId
        };
    }

    public async Task<PatientHealthSummary> GetPatientSummaryAsync(
        int patientId,
        int tenantId,
        SummaryDepth depth = SummaryDepth.Standard,
        CancellationToken ct = default)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId, ct);
        if (patient == null)
            throw new Exception("Patient not found");

        // Retrieve all patient context
        var contextDocuments = await RetrievePatientContextAsync(patientId, tenantId, "", fetchAll: true, ct: ct);

        var maxTokens = depth switch
        {
            SummaryDepth.Brief => 500,
            SummaryDepth.Comprehensive => 2000,
            _ => 1000
        };

        var summaryPrompt = $@"Patient: {patient.FirstName} {patient.LastName}
Date of Birth: {patient.DateOfBirth:yyyy-MM-dd}
Gender: {patient.Gender}

Generate a {(depth == SummaryDepth.Brief ? "brief" : "comprehensive")} health summary based on the following context:

{BuildContextString(contextDocuments)}

Provide:
1. A {(depth == SummaryDepth.Brief ? "brief" : "detailed")} clinical summary
2. List of active conditions (if known)
3. Current medications (if any)
4. Recent lab results (if any)
5. Any concerns or recommendations";

        var response = await _llmService.GenerateChatCompletionAsync(
            DoctorSystemPrompt,
            summaryPrompt,
            null,
            ct);

        // Extract medications and conditions from context
        var medications = contextDocuments
            .Where(d => d.SourceType == "Prescription")
            .Select(d => d.Content)
            .Distinct()
            .Take(10)
            .ToList();

        var labResults = contextDocuments
            .Where(d => d.SourceType == "VitalSign" && d.Content.Contains("glucose", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(d => d.Content)
            .ToList();

        return new PatientHealthSummary
        {
            PatientId = patientId,
            SummaryText = response.Content,
            ActiveConditions = ExtractConditions(contextDocuments),
            CurrentMedications = medications,
            RecentLabResults = labResults,
            UpcomingAppointments = new List<string>(), // Would need appointment lookup
            GeneratedAt = DateTime.UtcNow,
            Confidence = response.Confidence
        };
    }

    public async Task<AIConversationDto> GetOrCreateConversationAsync(
        int patientId,
        int userId,
        int tenantId,
        string? title = null,
        CancellationToken ct = default)
    {
        // Try to find existing conversation from today
        var existing = await _context.AIConversations
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenantId &&
                c.PatientId == patientId &&
                c.CreatedByUserId == userId &&
                c.IsActive &&
                c.CreatedAt.Date == DateTime.UtcNow.Date,
                ct);

        if (existing != null)
        {
            return new AIConversationDto
            {
                Id = existing.Id,
                PatientId = existing.PatientId,
                Title = existing.Title,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt,
                MessageCount = await _context.AIMessages.CountAsync(m => m.ConversationId == existing.Id, ct)
            };
        }

        // Create new conversation
        var conversation = new AIConversation
        {
            TenantId = tenantId,
            PatientId = patientId,
            CreatedByUserId = userId,
            Title = title ?? $"Health Chat - {DateTime.UtcNow:MMM dd}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AIConversations.Add(conversation);
        await _context.SaveChangesAsync(ct);

        return new AIConversationDto
        {
            Id = conversation.Id,
            PatientId = conversation.PatientId,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            MessageCount = 0
        };
    }

    public async Task<List<AIMessageDto>> GetConversationMessagesAsync(
        int conversationId,
        int tenantId,
        CancellationToken ct = default)
    {
        var conversation = await _context.AIMessages
            .Include(m => m.ConversationId)
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId, ct);

        if (conversation == null)
            return new List<AIMessageDto>();

        // Verify tenant access
        var conv = await _context.AIConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId, ct);
        if (conv == null)
            return new List<AIMessageDto>();

        var messages = await _context.AIMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return messages.Select(m => new AIMessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role.ToString(),
            Content = m.Content,
            Citations = string.IsNullOrEmpty(m.CitationsJson)
                ? null
                : JsonSerializer.Deserialize<List<RagCitationDto>>(m.CitationsJson),
            Confidence = m.Confidence,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    private async Task<List<RetrievedDocument>> RetrievePatientContextAsync(
        int patientId,
        int tenantId,
        string query,
        bool fetchAll = false,
        CancellationToken ct = default)
    {
        var documents = new List<RetrievedDocument>();

        // Get patient info
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId, ct);

        if (patient != null)
        {
            documents.Add(new RetrievedDocument
            {
                SourceType = "Patient",
                SourceId = patient.Id,
                SourceName = $"{patient.FirstName} {patient.LastName}",
                Content = $"Patient: {patient.FirstName} {patient.LastName}, DOB: {patient.DateOfBirth:yyyy-MM-dd}, Gender: {patient.Gender}, Blood Group: {patient.BloodGroup ?? "Unknown"}",
                RelevanceScore = 1.0f
            });
        }

        // Get medical notes
        var notes = await _context.MedicalNotes
            .Where(n => n.TenantId == tenantId && n.PatientId == patientId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var note in notes)
        {
            documents.Add(new RetrievedDocument
            {
                SourceType = "MedicalNote",
                SourceId = note.Id,
                SourceName = $"Clinical Note - {note.NoteType}",
                Content = $"[{note.NoteType}] {note.Content}",
                RelevanceScore = 0.9f
            });
        }

        // Get prescriptions
        var prescriptions = await _context.Prescriptions
            .Where(p => p.TenantId == tenantId && p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var rx in prescriptions)
        {
            documents.Add(new RetrievedDocument
            {
                SourceType = "Prescription",
                SourceId = rx.Id,
                SourceName = $"Prescription - {rx.CreatedAt:yyyy-MM-dd}",
                Content = $"Medications: {rx.Medications}. Instructions: {rx.Instructions}. Allergies noted: {rx.Allergies}",
                RelevanceScore = 0.9f
            });
        }

        // Get vital signs
        var vitals = await _context.VitalSigns
            .Where(v => v.TenantId == tenantId && v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var vital in vitals)
        {
            var content = $"Vital Signs ({vital.RecordedAt:yyyy-MM-dd}): ";
            if (vital.BloodPressureSystolic.HasValue)
                content += $"BP: {vital.BloodPressureSystolic}/{vital.BloodPressureDiastolic} mmHg, ";
            if (vital.HeartRate.HasValue)
                content += $"HR: {vital.HeartRate} bpm, ";
            if (vital.TemperatureCelsius.HasValue)
                content += $"Temp: {vital.TemperatureCelsius}°C, ";
            if (vital.WeightKg.HasValue)
                content += $"Weight: {vital.WeightKg} kg";

            documents.Add(new RetrievedDocument
            {
                SourceType = "VitalSign",
                SourceId = vital.Id,
                SourceName = $"Vital Signs - {vital.RecordedAt:yyyy-MM-dd}",
                Content = content,
                RelevanceScore = 0.8f
            });
        }

        // Get file attachments (descriptions only, not file content)
        var attachments = await _context.FileAttachments
            .Where(f => f.TenantId == tenantId && f.PatientId == patientId && f.IsActive)
            .OrderByDescending(f => f.UploadedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var att in attachments)
        {
            documents.Add(new RetrievedDocument
            {
                SourceType = "FileAttachment",
                SourceId = att.Id,
                SourceName = $"{att.Category}: {att.OriginalFileName}",
                Content = $"Uploaded report: {att.OriginalFileName} ({att.ContentType}). Description: {att.Description ?? "No description"}",
                RelevanceScore = 0.7f
            });
        }

        return documents;
    }

    private string BuildContextString(List<RetrievedDocument> documents)
    {
        if (!documents.Any())
            return "No patient records found.";

        var sb = new StringBuilder();
        sb.AppendLine("=== PATIENT RECORDS ===\n");

        var grouped = documents.GroupBy(d => d.SourceType);
        foreach (var group in grouped)
        {
            sb.AppendLine($"--- {group.Key}s ---");
            foreach (var doc in group.Take(5))
            {
                sb.AppendLine($"* {doc.SourceName}");
                sb.AppendLine($"  {doc.Content}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private List<string> GenerateSuggestedActions(string query, List<RetrievedDocument> documents)
    {
        var actions = new List<string>();

        // Generic suggestions based on query content
        if (query.Contains("medication", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("medicine", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("Schedule an appointment to discuss medications");
        }

        if (query.Contains("symptom", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("pain", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("Consider booking an appointment for evaluation");
        }

        if (query.Contains("test", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("result", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("Review recent lab or test results with your doctor");
        }

        actions.Add("Contact your healthcare provider for personalized advice");

        return actions;
    }

    private List<string> ExtractConditions(List<RetrievedDocument> documents)
    {
        // Simple extraction from medical notes
        return documents
            .Where(d => d.SourceType == "MedicalNote" && !string.IsNullOrEmpty(d.Content))
            .Select(d => d.Content)
            .Where(c => c.Contains("diagnosis", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("condition", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("assessment", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();
    }

    private class RetrievedDocument
    {
        public string SourceType { get; set; } = string.Empty;
        public int SourceId { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float RelevanceScore { get; set; }
    }
}
