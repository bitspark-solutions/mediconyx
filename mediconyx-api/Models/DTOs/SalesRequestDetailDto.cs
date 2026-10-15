namespace mediconyx_api.Models.DTOs;

/// <summary>
/// Flat DTO for the sales-request Detail view.
/// Returns enums as their numeric value (the sales UI compares numerically)
/// and flattens navigation collections to avoid JSON object cycles.
/// </summary>
public class SalesRequestDetailDto
{
    public int Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Notes { get; set; }

    // Numeric enum values (frontend statusLabels / paymentStatusLabels are number-keyed)
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int PaymentStatus { get; set; }
    public string PaymentStatusName { get; set; } = string.Empty;

    public int? OwnerSalesUserId { get; set; }
    public string? OwnerSalesUserName { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    public string? SubscriptionPlan { get; set; }
    public int? BillingCycle { get; set; }
    public string? Currency { get; set; }
    public decimal? QuotedPrice { get; set; }
    public int? Seats { get; set; }
    public DateTime? PaidAt { get; set; }

    public int? TenantId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<SalesRequestAttachmentDto> Attachments { get; set; } = new();
    public List<SalesRequestNoteDto> NotesList { get; set; } = new();
    public List<SalesRequestTimelineDto> Timeline { get; set; } = new();
}
