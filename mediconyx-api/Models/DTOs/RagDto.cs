namespace mediconyx_api.Models.DTOs;

public class RagQueryRequest
{
    public string Message { get; set; } = string.Empty;
    public int? ConversationId { get; set; }
}

public class RagQueryResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<RagCitationDto> Citations { get; set; } = new();
    public float Confidence { get; set; }
    public List<string> SuggestedActions { get; set; } = new();
    public int ConversationId { get; set; }
    public int MessageId { get; set; }
}

public class RagCitationDto
{
    public string SourceType { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
}

public class PatientHealthSummary
{
    public int PatientId { get; set; }
    public string SummaryText { get; set; } = string.Empty;
    public List<string> ActiveConditions { get; set; } = new();
    public List<string> CurrentMedications { get; set; } = new();
    public List<string> RecentLabResults { get; set; } = new();
    public List<string> UpcomingAppointments { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public float Confidence { get; set; }
}

public class AIConversationDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

public class AIMessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<RagCitationDto>? Citations { get; set; }
    public float? Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationListResponse
{
    public List<AIConversationDto> Conversations { get; set; } = new();
    public int TotalCount { get; set; }
}
