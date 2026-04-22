namespace mediconyx_api.Models.DTOs;

public class ModuleSelectionResponseDto : ModuleSelectionDto
{
    public int Id { get; set; }
    public int SalesRequestId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
