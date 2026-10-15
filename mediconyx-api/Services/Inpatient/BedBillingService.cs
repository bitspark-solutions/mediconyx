using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Inpatient;

public class BedBillingService : IBedBillingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BedBillingService> _logger;

    public BedBillingService(ApplicationDbContext context, ILogger<BedBillingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BillingResult> GenerateForAdmissionAsync(int admissionId, int tenantId, int userId, CancellationToken ct = default)
    {
        var admission = await _context.Admissions
            .Include(a => a.Transfers)
            .FirstOrDefaultAsync(a => a.Id == admissionId && a.TenantId == tenantId, ct);
        if (admission == null) throw new KeyNotFoundException($"Admission {admissionId} not found.");

        // Billing window: from admission date to discharge date (or now if still active).
        var fromDate = admission.AdmissionDate.Date;
        var toDate = (admission.DischargeDate ?? DateTime.UtcNow).Date;

        // Walk every day in the window. Resolve which bed was occupied that day.
        // For days BEFORE the first transfer, the bed is admission.Bed.
        // For days ON/AFTER a transfer, the bed is the destination of the latest transfer <= that day.
        var orderedTransfers = admission.Transfers.OrderBy(t => t.TransferredAt).ToList();

        // Pre-load all the relevant beds (current + all transfer destinations) with their wards.
        var bedIds = new HashSet<int> { admission.BedId };
        bedIds.UnionWith(orderedTransfers.Select(t => t.ToBedId));
        var beds = await _context.Beds.Include(b => b.Ward)
            .Where(b => bedIds.Contains(b.Id) && b.TenantId == tenantId)
            .ToDictionaryAsync(b => b.Id, ct);

        // Existing charges (for idempotency)
        var existing = await _context.BedCharges
            .Where(c => c.AdmissionId == admissionId)
            .Select(c => c.ChargeDate.Date)
            .ToListAsync(ct);
        var existingDates = new HashSet<DateTime>(existing);

        var newRows = new List<BedCharge>();
        var totalAmount = await _context.BedCharges
            .Where(c => c.AdmissionId == admissionId)
            .SumAsync(c => c.Amount, ct);

        var totalDays = 0;
        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            totalDays++;
            if (existingDates.Contains(day)) continue;

            var bedIdForDay = ResolveBedForDay(admission, orderedTransfers, day);
            if (!beds.TryGetValue(bedIdForDay, out var bed))
            {
                _logger.LogWarning("Bed {BedId} not found for admission {AdmissionId} on {Day}; skipping.", bedIdForDay, admissionId, day);
                continue;
            }

            var rate = bed.DailyRate ?? bed.Ward.DefaultDailyRate;
            if (rate <= 0)
            {
                // No rate configured -- still record a zero-amount charge so the day is "claimed" and won't be re-evaluated.
                _logger.LogInformation("No rate set for bed {BedId} on {Day}; recording zero charge.", bedIdForDay, day);
            }

            var charge = new BedCharge
            {
                TenantId = tenantId,
                AdmissionId = admissionId,
                BedId = bedIdForDay,
                BedIdOnDay = bedIdForDay,
                ChargeDate = day,
                DailyRate = rate,
                Amount = rate,
                Description = $"{bed.Ward.WardType} - {bed.Ward.Name} - Bed {bed.Number} - {day:yyyy-MM-dd}",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            newRows.Add(charge);
            totalAmount += rate;
        }

        if (newRows.Count > 0)
        {
            _context.BedCharges.AddRange(newRows);
            await _context.SaveChangesAsync(ct);
        }

        return new BillingResult
        {
            AdmissionId = admissionId,
            From = fromDate,
            To = toDate,
            NewChargesCreated = newRows.Count,
            TotalChargeDays = totalDays,
            TotalAmount = totalAmount
        };
    }

    public async Task<AdmissionBillDto> GetBillAsync(int admissionId, int tenantId, CancellationToken ct = default)
    {
        var admission = await _context.Admissions
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == admissionId && a.TenantId == tenantId, ct);
        if (admission == null) throw new KeyNotFoundException($"Admission {admissionId} not found.");

        var charges = await _context.BedCharges
            .Include(c => c.Bed).ThenInclude(b => b.Ward)
            .Where(c => c.AdmissionId == admissionId)
            .OrderBy(c => c.ChargeDate)
            .ToListAsync(ct);

        return new AdmissionBillDto
        {
            AdmissionId = admissionId,
            TenantId = tenantId,
            PatientId = admission.PatientId,
            PatientName = $"{admission.Patient.FirstName} {admission.Patient.LastName}",
            PatientNumber = admission.Patient.PatientNumber,
            AdmissionDate = admission.AdmissionDate,
            DischargeDate = admission.DischargeDate,
            TotalDays = charges.Count,
            TotalAmount = charges.Sum(c => c.Amount),
            Currency = "BDT",
            Charges = charges.Select(c => new BedChargeDto
            {
                Id = c.Id,
                ChargeDate = c.ChargeDate,
                BedId = c.BedId,
                BedNumber = c.Bed.Number,
                WardName = c.Bed.Ward.Name,
                WardType = c.Bed.Ward.WardType,
                DailyRate = c.DailyRate,
                Amount = c.Amount,
                Description = c.Description
            }).ToList()
        };
    }

    /// <summary>
    /// Which bed was the patient in on this day? Walk the transfers: the bed is
    /// admission.Bed for days before the first transfer, then the ToBed of the
    /// latest transfer on or before this day.
    /// </summary>
    private static int ResolveBedForDay(Admission admission, List<AdmissionTransfer> orderedTransfers, DateTime day)
    {
        var bedId = admission.BedId;
        foreach (var t in orderedTransfers)
        {
            if (t.TransferredAt.Date <= day) bedId = t.ToBedId;
            else break;
        }
        return bedId;
    }
}
