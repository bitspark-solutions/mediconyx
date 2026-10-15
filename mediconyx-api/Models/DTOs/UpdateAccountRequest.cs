namespace mediconyx_api.Models.DTOs;

public class UpdateAccountRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? CostCenterId { get; set; }
    public bool? IsActive { get; set; }
}
