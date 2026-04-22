namespace mediconyx_api.Models.DTOs;

public class RejectProfileRequest
{
    public string Reason { get; set; } = string.Empty;
    public List<string> RequiredChanges { get; set; } = new();
}
