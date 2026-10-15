namespace mediconyx_api.Models.DTOs;

public class AIConversationDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}
