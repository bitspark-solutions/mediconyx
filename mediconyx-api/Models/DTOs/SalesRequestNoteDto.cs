namespace mediconyx_api.Models.DTOs;

public class SalesRequestNoteDto
{
    public int Id { get; set; }
    public string Note { get; set; } = string.Empty;
    public int AuthorUserId { get; set; }
    public string? AuthorUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
