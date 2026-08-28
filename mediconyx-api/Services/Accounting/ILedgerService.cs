using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Accounting;

public interface ILedgerService
{
    /// <summary>Post a balanced journal entry with its debit/credit lines.</summary>
    Task<JournalEntryDto> PostJournalEntryAsync(int tenantId, int createdByUserId, CreateJournalEntryRequest request);

    /// <summary>Create a reversing entry for a posted journal entry.</summary>
    Task<JournalEntryDto> ReverseJournalEntryAsync(int tenantId, int journalEntryId, int userId, string reason);

    /// <summary>List journal entries with pagination and filters.</summary>
    Task<(List<JournalEntryDto> Entries, int TotalCount)> GetJournalEntriesAsync(
        int tenantId, int? branchId, int? fiscalPeriodId,
        JournalEntrySourceType? sourceType, DateTime? dateFrom, DateTime? dateTo,
        int pageNumber, int pageSize);

    /// <summary>Get a single journal entry with its postings.</summary>
    Task<JournalEntryDto?> GetJournalEntryByIdAsync(int tenantId, int journalEntryId);

    /// <summary>Generate a Trial Balance for a fiscal period.</summary>
    Task<TrialBalanceDto> GetTrialBalanceAsync(int tenantId, int fiscalPeriodId);

    /// <summary>Generate a Balance Sheet as of a specific date.</summary>
    Task<BalanceSheetDto> GetBalanceSheetAsync(int tenantId, DateTime asOfDate);

    /// <summary>Generate an Income Statement for a date range.</summary>
    Task<IncomeStatementDto> GetIncomeStatementAsync(int tenantId, DateTime periodStart, DateTime periodEnd);

    /// <summary>Close a fiscal period — prevents further postings.</summary>
    Task CloseFiscalPeriodAsync(int tenantId, int fiscalPeriodId, int userId);

    /// <summary>Get the Chart of Accounts as a tree structure.</summary>
    Task<List<AccountDto>> GetChartOfAccountsAsync(int tenantId);

    /// <summary>Create a new account in the Chart of Accounts.</summary>
    Task<AccountDto> CreateAccountAsync(int tenantId, CreateAccountRequest request);

    /// <summary>Update an existing account (name, description, active status only).</summary>
    Task<AccountDto> UpdateAccountAsync(int tenantId, int accountId, UpdateAccountRequest request);

    /// <summary>Get all fiscal periods for a tenant.</summary>
    Task<List<FiscalPeriodDto>> GetFiscalPeriodsAsync(int tenantId);

    /// <summary>Find the fiscal period that contains the given date.</summary>
    Task<FiscalPeriod?> FindFiscalPeriodForDateAsync(int tenantId, DateTime date);
}

public class FiscalPeriodDto
{
    public int Id { get; set; }
    public int FiscalYearId { get; set; }
    public int Year { get; set; }
    public int PeriodNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
}
