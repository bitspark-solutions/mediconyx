namespace mediconyx_api.Models.DTOs;

public class ConversationListResponse
{
    public List<AIConversationDto> Conversations { get; set; } = new();
    public int TotalCount { get; set; }
}
