namespace mediconyx_api.Models.DTOs;

/// <summary>DTO for Chart of Accounts tree node.</summary>
public class AccountDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string NormalBalance { get; set; } = string.Empty;
    public int? ParentAccountId { get; set; }
    public string? ParentAccountName { get; set; }
    public bool IsSystemAccount { get; set; }
    public int? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public decimal CurrentBalance { get; set; }
    public List<AccountDto> Children { get; set; } = new();
}
