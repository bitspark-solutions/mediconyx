using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A node in the hierarchical Chart of Accounts (CoA).
/// Each tenant gets a full set of accounts seeded on provisioning.
/// Accounts form a tree via ParentAccountId (e.g. "Current Assets" → "Cash on Hand").
/// System accounts (IsSystemAccount=true) are seeded and cannot be deleted.
/// </summary>
public class Account
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>
    /// Unique code within the tenant, e.g. "1010", "4020".
    /// Follows standard block coding: 1xxx=Asset, 2xxx=Liability, 3xxx=Equity,
    /// 4xxx=Revenue, 49xx=ContraRevenue, 5xxx=Expense.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    /// <summary>
    /// Derived from AccountType. Asset/Expense/ContraRevenue = Debit; Liability/Equity/Revenue = Credit.
    /// </summary>
    public NormalBalance NormalBalance { get; set; }

    /// <summary>
    /// Self-referencing FK for tree hierarchy. Null for root-level accounts (e.g. "Current Assets").
    /// </summary>
    public int? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }

    /// <summary>
    /// If true, this account was seeded by the system and cannot be deleted.
    /// Users can still rename or deactivate it.
    /// </summary>
    public bool IsSystemAccount { get; set; }

    /// <summary>
    /// Optional link to a Department for cost-center/departmental P&L tracking.
    /// </summary>
    public int? CostCenterId { get; set; }
    public Department? CostCenter { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Account> ChildAccounts { get; set; } = new List<Account>();
    public ICollection<JournalPosting> Postings { get; set; } = new List<JournalPosting>();
}
