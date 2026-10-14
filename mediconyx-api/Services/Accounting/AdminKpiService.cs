using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Accounting;

/// <summary>
/// Provides executive dashboard KPIs derived from the accounting ledger
/// and clinical operational data (admissions, discharges, mortality, births).
/// </summary>
public class AdminKpiService : IAdminKpiService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminKpiService> _logger;

    public AdminKpiService(ApplicationDbContext context, ILogger<AdminKpiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdminFinancialKpiDto> GetFinancialKpisAsync(int tenantId, AdminKpiQueryParams query)
    {
        var (dateFrom, dateTo, dateLabel) = ResolveDateRange(query);

        // Revenue from ledger: sum of credits to Revenue accounts (4xxx) in the period
        var revenuePostings = await _context.JournalPostings
            .Include(p => p.JournalEntry)
            .Include(p => p.Account)
            .Where(p => p.JournalEntry.TenantId == tenantId
                     && p.JournalEntry.IsPosted
                     && p.JournalEntry.EntryDate >= dateFrom
                     && p.JournalEntry.EntryDate <= dateTo
                     && p.Account.AccountType == AccountType.Revenue)
            .ToListAsync();

        var totalRevenue = revenuePostings.Sum(p => p.CreditAmount - p.DebitAmount);

        // Expenses from ledger: sum of debits to Expense accounts (5xxx)
        var expensePostings = await _context.JournalPostings
            .Include(p => p.JournalEntry)
            .Include(p => p.Account)
            .Where(p => p.JournalEntry.TenantId == tenantId
                     && p.JournalEntry.IsPosted
                     && p.JournalEntry.EntryDate >= dateFrom
                     && p.JournalEntry.EntryDate <= dateTo
                     && p.Account.AccountType == AccountType.Expense)
            .ToListAsync();

        var totalExpenses = expensePostings.Sum(p => p.DebitAmount - p.CreditAmount);

        // Contra-revenue deductions
        var contraPostings = await _context.JournalPostings
            .Include(p => p.JournalEntry)
            .Include(p => p.Account)
            .Where(p => p.JournalEntry.TenantId == tenantId
                     && p.JournalEntry.IsPosted
                     && p.JournalEntry.EntryDate >= dateFrom
                     && p.JournalEntry.EntryDate <= dateTo
                     && p.Account.AccountType == AccountType.ContraRevenue)
            .ToListAsync();

        var totalContraRevenue = contraPostings.Sum(p => p.DebitAmount - p.CreditAmount);
        var netRevenue = totalRevenue - totalContraRevenue;
        var netProfit = netRevenue - totalExpenses;

        // AR balance (current): sum of debits - credits on AR accounts
        var arAccounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId && (a.Code == "1030" || a.Code == "1031") && a.IsActive)
            .Select(a => a.Id)
            .ToListAsync();

        var arBalance = 0m;
        if (arAccounts.Any())
        {
            var arPostings = await _context.JournalPostings
                .Include(p => p.JournalEntry)
                .Where(p => p.JournalEntry.TenantId == tenantId
                         && p.JournalEntry.IsPosted
                         && arAccounts.Contains(p.AccountId))
                .ToListAsync();
            arBalance = arPostings.Sum(p => p.DebitAmount - p.CreditAmount);
        }

        // Cash on hand: sum of debits - credits on cash accounts
        var cashAccounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId && (a.Code == "1010" || a.Code == "1020") && a.IsActive)
            .Select(a => a.Id)
            .ToListAsync();

        var cashOnHand = 0m;
        if (cashAccounts.Any())
        {
            var cashPostings = await _context.JournalPostings
                .Include(p => p.JournalEntry)
                .Where(p => p.JournalEntry.TenantId == tenantId
                         && p.JournalEntry.IsPosted
                         && cashAccounts.Contains(p.AccountId))
                .ToListAsync();
            cashOnHand = cashPostings.Sum(p => p.DebitAmount - p.CreditAmount);
        }

        // AR Days = (AR Balance / Net Revenue) × Days in period
        var daysInPeriod = (dateTo - dateFrom).Days;
        var arDays = netRevenue > 0
            ? (arBalance / netRevenue) * daysInPeriod
            : 0;

        // If no ledger data, fallback to invoice-based revenue
        if (totalRevenue == 0)
        {
            var invoiceRevenue = await _context.Invoices
                .Where(i => i.TenantId == tenantId && i.PaidAt >= dateFrom && i.PaidAt <= dateTo)
                .SumAsync(i => i.PaidAmount);
            totalRevenue = invoiceRevenue;
            netRevenue = invoiceRevenue;
            netProfit = invoiceRevenue; // No expense data without ledger

            var outstanding = await _context.Invoices
                .Where(i => i.TenantId == tenantId
                         && i.Status != InvoiceStatus.Paid
                         && i.Status != InvoiceStatus.Cancelled
                         && i.Status != InvoiceStatus.Refunded)
                .SumAsync(i => i.TotalAmount - i.PaidAmount);
            arBalance = outstanding;
        }

        return new AdminFinancialKpiDto
        {
            TotalRevenue = netRevenue,
            OperatingExpenses = totalExpenses,
            NetOperatingProfit = netProfit,
            OperatingMarginPercent = netRevenue > 0 ? (netProfit / netRevenue) * 100 : 0,
            AccountsReceivable = arBalance,
            ArDays = arDays,
            CashOnHand = cashOnHand,
            DateRangeLabel = dateLabel
        };
    }

    public async Task<AdminClinicalKpiDto> GetClinicalKpisAsync(int tenantId, AdminKpiQueryParams query)
    {
        var (dateFrom, dateTo, _) = ResolveDateRange(query);

        // Active admissions
        var activeAdmissions = await _context.Admissions
            .CountAsync(a => a.TenantId == tenantId && a.Status == AdmissionStatus.Active);

        // Total beds
        var totalBeds = await _context.Beds
            .Include(b => b.Ward)
            .CountAsync(b => b.Ward.TenantId == tenantId && b.IsActive);

        var bedOccupancy = totalBeds > 0 ? ((decimal)activeAdmissions / totalBeds) * 100 : 0;

        // Discharged patients in the period
        var dischargedQuery = _context.Admissions
            .Where(a => a.TenantId == tenantId
                     && a.Status == AdmissionStatus.Discharged
                     && a.DischargeDate >= dateFrom
                     && a.DischargeDate <= dateTo);

        var discharged = await dischargedQuery.ToListAsync();
        var dischargedCount = discharged.Count;

        // Average Length of Stay (ALOS) for discharged patients
        var alos = discharged.Any()
            ? (decimal)discharged.Average(a =>
                (a.DischargeDate!.Value - a.AdmissionDate).TotalDays)
            : 0;

        // Deceased patients in the period
        var deceasedCount = await _context.Admissions
            .CountAsync(a => a.TenantId == tenantId
                          && a.Status == AdmissionStatus.Deceased
                          && a.DischargeDate >= dateFrom
                          && a.DischargeDate <= dateTo);

        // Total admissions resolved in period (for mortality rate denominator)
        var totalResolved = await _context.Admissions
            .CountAsync(a => a.TenantId == tenantId
                          && a.DischargeDate >= dateFrom
                          && a.DischargeDate <= dateTo);
        var mortalityRate = totalResolved > 0 ? ((decimal)deceasedCount / totalResolved) * 100 : 0;

        // New births: admissions to Maternity wards in the period
        var maternityWardIds = await _context.Wards
            .Where(w => w.TenantId == tenantId && w.WardType == WardTypes.Maternity)
            .Select(w => w.Id)
            .ToListAsync();

        var newBirths = 0;
        if (maternityWardIds.Any())
        {
            var maternityBedIds = await _context.Beds
                .Where(b => maternityWardIds.Contains(b.WardId))
                .Select(b => b.Id)
                .ToListAsync();

            newBirths = await _context.Admissions
                .CountAsync(a => a.TenantId == tenantId
                              && maternityBedIds.Contains(a.BedId)
                              && a.AdmissionDate >= dateFrom
                              && a.AdmissionDate <= dateTo);
        }

        return new AdminClinicalKpiDto
        {
            ActiveAdmissions = activeAdmissions,
            TotalBeds = totalBeds,
            BedOccupancyPercent = bedOccupancy,
            DischargedPatients = dischargedCount,
            AverageLengthOfStay = Math.Round(alos, 1),
            DeceasedPatients = deceasedCount,
            MortalityRatePercent = Math.Round(mortalityRate, 2),
            NewBirths = newBirths
        };
    }

    public async Task<List<DepartmentalRevenueDto>> GetDepartmentalRevenueAsync(
        int tenantId, AdminKpiQueryParams query)
    {
        var (dateFrom, dateTo, _) = ResolveDateRange(query);

        // Revenue by department (via CostCenterId on Account)
        var revenueByDept = await _context.JournalPostings
            .Include(p => p.JournalEntry)
            .Include(p => p.Account).ThenInclude(a => a.CostCenter)
            .Where(p => p.JournalEntry.TenantId == tenantId
                     && p.JournalEntry.IsPosted
                     && p.JournalEntry.EntryDate >= dateFrom
                     && p.JournalEntry.EntryDate <= dateTo
                     && p.Account.AccountType == AccountType.Revenue)
            .GroupBy(p => new { p.Account.CostCenterId, DeptName = p.Account.CostCenter!.Name })
            .Select(g => new DepartmentalRevenueDto
            {
                DepartmentId = g.Key.CostCenterId,
                DepartmentName = g.Key.DeptName ?? "Unallocated",
                Revenue = g.Sum(p => p.CreditAmount - p.DebitAmount)
            })
            .ToListAsync();

        // If no ledger data, fallback to invoice-based grouping by department
        if (!revenueByDept.Any())
        {
            revenueByDept = await _context.InvoiceLineItems
                .Include(li => li.Department)
                .Include(li => li.Invoice)
                .Where(li => li.TenantId == tenantId
                          && li.Invoice.PaidAt >= dateFrom
                          && li.Invoice.PaidAt <= dateTo)
                .GroupBy(li => new { li.DepartmentId, DeptName = li.Department!.Name })
                .Select(g => new DepartmentalRevenueDto
                {
                    DepartmentId = g.Key.DepartmentId,
                    DepartmentName = g.Key.DeptName ?? "General",
                    Revenue = g.Sum(li => li.NetAmount)
                })
                .ToListAsync();
        }

        return revenueByDept.OrderByDescending(d => d.Revenue).ToList();
    }

    public async Task<List<RevenueTrendDto>> GetRevenueTrendAsync(
        int tenantId, AdminKpiQueryParams query)
    {
        var (dateFrom, dateTo, _) = ResolveDateRange(query);
        var granularity = query.Granularity?.ToLower() ?? "daily";

        // Get all revenue and expense postings in range
        var postings = await _context.JournalPostings
            .Include(p => p.JournalEntry)
            .Include(p => p.Account)
            .Where(p => p.JournalEntry.TenantId == tenantId
                     && p.JournalEntry.IsPosted
                     && p.JournalEntry.EntryDate >= dateFrom
                     && p.JournalEntry.EntryDate <= dateTo
                     && (p.Account.AccountType == AccountType.Revenue
                      || p.Account.AccountType == AccountType.Expense))
            .Select(p => new
            {
                Date = p.JournalEntry.EntryDate,
                IsRevenue = p.Account.AccountType == AccountType.Revenue,
                Amount = p.Account.AccountType == AccountType.Revenue
                    ? p.CreditAmount - p.DebitAmount
                    : p.DebitAmount - p.CreditAmount
            })
            .ToListAsync();

        // Group by period
        var grouped = postings.GroupBy(p => granularity switch
        {
            "weekly" => $"{p.Date:yyyy}-W{System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(p.Date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday):D2}",
            "monthly" => p.Date.ToString("yyyy-MM"),
            _ => p.Date.ToString("yyyy-MM-dd")
        });

        return grouped
            .Select(g =>
            {
                var revenue = g.Where(p => p.IsRevenue).Sum(p => p.Amount);
                var expenses = g.Where(p => !p.IsRevenue).Sum(p => p.Amount);
                return new RevenueTrendDto
                {
                    Period = g.Key,
                    Revenue = revenue,
                    Expenses = expenses,
                    Profit = revenue - expenses
                };
            })
            .OrderBy(r => r.Period)
            .ToList();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static (DateTime From, DateTime To, string Label) ResolveDateRange(AdminKpiQueryParams query)
    {
        var today = DateTime.UtcNow.Date;

        if (query.DateFrom.HasValue && query.DateTo.HasValue)
            return (query.DateFrom.Value, query.DateTo.Value,
                $"{query.DateFrom.Value:MMM dd} – {query.DateTo.Value:MMM dd, yyyy}");

        return query.DateRange?.ToLower() switch
        {
            "today" => (today, today.AddDays(1).AddTicks(-1), "Today"),
            "week" => (today.AddDays(-(int)today.DayOfWeek + 1),
                       today.AddDays(1).AddTicks(-1), "This Week"),
            "month" => (new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                        today.AddDays(1).AddTicks(-1), "This Month"),
            "year" => (new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                       today.AddDays(1).AddTicks(-1), "This Year"),
            _ => (new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                  today.AddDays(1).AddTicks(-1), "This Month")
        };
    }
}
