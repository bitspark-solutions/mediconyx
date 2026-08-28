namespace mediconyx_api.Models;

/// <summary>
/// Status of a fiscal accounting period. Controls whether journal entries
/// can be posted to this period.
/// </summary>
public enum FiscalPeriodStatus
{
    /// <summary>Period is open — journal entries can be posted.</summary>
    Open = 0,

    /// <summary>Period has been closed — no new postings allowed. Admin can re-open.</summary>
    Closed = 1,

    /// <summary>Period is permanently locked — cannot be re-opened without system override.</summary>
    Locked = 2
}
