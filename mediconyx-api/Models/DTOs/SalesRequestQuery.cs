namespace mediconyx_api.Models.DTOs;

using mediconyx_api.Models;

public class SalesRequestQuery
{
    public SalesRequestStatus? Status { get; set; }
    public int? OwnerId { get; set; }
    public string? Region { get; set; }
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
