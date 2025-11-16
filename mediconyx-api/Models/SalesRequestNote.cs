namespace mediconyx_api.Models;

public class SalesRequestNote
{
    public int Id { get; set; }
    public int SalesRequestId { get; set; }
    public SalesRequest SalesRequest { get; set; } = null!;
    public int AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
