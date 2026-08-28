using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A structured line item on an invoice. Parallel to the existing Invoice.LineItems
/// plain-text field — this provides queryable, typed data linked to the Chart of Accounts
/// for automatic journal entry generation.
/// </summary>
public class InvoiceLineItem
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [Required]
    [StringLength(300)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category of service: "BedCharge", "Consultation", "Pharmacy", "Lab",
    /// "Surgery", "Emergency", "Radiology", "Maternity", "ICU", "Other".
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ServiceType { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }

    /// <summary>Quantity × UnitPrice before discount and tax.</summary>
    public decimal Amount { get; set; }

    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }

    /// <summary>Amount − DiscountAmount + TaxAmount.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// The revenue account to credit when this line item generates a journal entry.
    /// e.g. "Revenue – Bed Charges" (4010) for bed charge items.
    /// </summary>
    public int? RevenueAccountId { get; set; }
    public Account? RevenueAccount { get; set; }

    /// <summary>Optional department for cost-center tracking.</summary>
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    /// <summary>Link to admission for IPD charges.</summary>
    public int? AdmissionId { get; set; }

    /// <summary>Link to appointment for OPD charges.</summary>
    public int? AppointmentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
