using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Inpatient;

/// <summary>
/// Generates per-bed-day BedCharge rows for admissions. Idempotent: re-running
/// for the same admission never double-charges a day. Handles mid-admission
/// transfers by recording which bed was occupied on the charge date.
/// </summary>
public interface IBedBillingService
{
    /// <summary>
    /// Generates BedCharge rows from AdmissionDate to min(now, DischargeDate).
    /// Returns the number of new rows created (0 = already up to date).
    /// </summary>
    Task<BillingResult> GenerateForAdmissionAsync(int admissionId, int tenantId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the full bill (all charges + total) for an admission.
    /// </summary>
    Task<AdmissionBillDto> GetBillAsync(int admissionId, int tenantId, CancellationToken ct = default);
}
