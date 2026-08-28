namespace mediconyx_api.Models;

/// <summary>
/// Identifies the origin of a journal entry for audit and traceability.
/// </summary>
public enum JournalEntrySourceType
{
    Manual = 0,
    InvoiceIssued = 1,
    InvoicePayment = 2,
    BedChargeGeneration = 3,
    PayrollRun = 4,
    Adjustment = 5,
    Reversal = 6,
    OpeningBalance = 7,
    YearEndClose = 8,
    Refund = 9,
    BadDebtWriteOff = 10,
    DiscountApplied = 11
}
