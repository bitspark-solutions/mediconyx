namespace mediconyx_api.Models.DTOs;

using mediconyx_api.Models;

public class SalesStatusUpdate
{
    public SalesRequestStatus Status { get; set; }
    public string? Reason { get; set; }
}
