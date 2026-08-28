namespace mediconyx_api.Models.DTOs;

public class CreateAccountRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public int? ParentAccountId { get; set; }
    public int? CostCenterId { get; set; }
    public string? Description { get; set; }
}

public class UpdateAccountRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? CostCenterId { get; set; }
    public bool? IsActive { get; set; }
}
