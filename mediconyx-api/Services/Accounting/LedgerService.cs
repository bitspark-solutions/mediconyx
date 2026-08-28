using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Accounting;

public class LedgerService : ILedgerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(ApplicationDbContext context, ILogger<LedgerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Journal Entry Posting ──────────────────────────────────────────────

    public async Task<JournalEntryDto> PostJournalEntryAsync(
        int tenantId, int createdByUserId, CreateJournalEntryRequest request)
    {
        // Validate postings exist
        if (request.Postings == null || request.Postings.Count < 2)
            throw new ArgumentException("A journal entry must have at least two posting lines.");

        // Validate balance: SUM(debits) must equal SUM(credits)
        var totalDebit = request.Postings.Sum(p => p.DebitAmount);
        var totalCredit = request.Postings.Sum(p => p.CreditAmount);
        if (Math.Abs(totalDebit - totalCredit) > 0.0001m)
            throw new ArgumentException(
                $"Journal entry is not balanced. Debits: {totalDebit:F4}, Credits: {totalCredit:F4}");

        // Validate each posting has exactly one non-zero side
        foreach (var p in request.Postings)
        {
            if (p.DebitAmount < 0 || p.CreditAmount < 0)
                throw new ArgumentException("Amounts cannot be negative.");
            if (p.DebitAmount > 0 && p.CreditAmount > 0)
                throw new ArgumentException("A posting line cannot have both debit and credit amounts.");
            if (p.DebitAmount == 0 && p.CreditAmount == 0)
                throw new ArgumentException("A posting line must have a non-zero debit or credit amount.");
        }

        // Find the fiscal period for the entry date
        var fiscalPeriod = await FindFiscalPeriodForDateAsync(tenantId, request.EntryDate)
            ?? throw new InvalidOperationException(
                $"No open fiscal period found for date {request.EntryDate:yyyy-MM-dd}. " +
                "Please create or open the appropriate fiscal period.");

        if (fiscalPeriod.Status != FiscalPeriodStatus.Open)
            throw new InvalidOperationException(
                $"Fiscal period {fiscalPeriod.PeriodNumber}/{fiscalPeriod.FiscalYear.Year} is {fiscalPeriod.Status}. " +
                "Cannot post journal entries to a closed or locked period.");

        // Resolve branch
        Branch branch;
        if (request.BranchId.HasValue)
        {
            branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == request.BranchId.Value && b.TenantId == tenantId && b.IsActive)
                ?? throw new ArgumentException("Branch not found in this tenant.");
        }
        else
        {
            branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.IsMain)
                ?? throw new ArgumentException("No main branch configured for this tenant.");
        }

        // Validate all account IDs belong to this tenant
        var accountIds = request.Postings.Select(p => p.AccountId).Distinct().ToList();
        var accounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId && accountIds.Contains(a.Id) && a.IsActive)
            .ToDictionaryAsync(a => a.Id);

        if (accounts.Count != accountIds.Count)
        {
            var missing = accountIds.Except(accounts.Keys);
            throw new ArgumentException($"Account(s) not found or inactive: {string.Join(", ", missing)}");
        }

        // Generate entry number
        var entryNumber = await GenerateEntryNumberAsync(tenantId, request.EntryDate);

        // Create the journal entry
        var entry = new JournalEntry
        {
            TenantId = tenantId,
            BranchId = branch.Id,
            EntryNumber = entryNumber,
            EntryDate = request.EntryDate,
            Description = request.Description.Trim(),
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            FiscalPeriodId = fiscalPeriod.Id,
            IsPosted = true, // Auto-post (no draft state for now)
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        // Create postings
        foreach (var p in request.Postings)
        {
            entry.Postings.Add(new JournalPosting
            {
                AccountId = p.AccountId,
                DebitAmount = p.DebitAmount,
                CreditAmount = p.CreditAmount,
                Memo = p.Memo?.Trim()
            });
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Update materialized account balances
        await UpdateAccountBalancesAsync(tenantId, fiscalPeriod.Id, entry.Postings);

        _logger.LogInformation(
            "Journal entry {EntryNumber} posted for Tenant {TenantId}: {TotalDebit:F2} debit / {TotalCredit:F2} credit",
            entryNumber, tenantId, totalDebit, totalCredit);

        return await MapToJournalEntryDtoAsync(entry);
    }

    public async Task<JournalEntryDto> ReverseJournalEntryAsync(
        int tenantId, int journalEntryId, int userId, string reason)
    {
        var original = await _context.JournalEntries
            .Include(e => e.Postings)
            .FirstOrDefaultAsync(e => e.Id == journalEntryId && e.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Journal entry {journalEntryId} not found.");

        if (!original.IsPosted)
            throw new InvalidOperationException("Cannot reverse a draft journal entry.");
        if (original.IsReversed)
            throw new InvalidOperationException("This journal entry has already been reversed.");

        // Create the reversal request with opposite debit/credit
        var reversalRequest = new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow,
            Description = $"REVERSAL of {original.EntryNumber}: {reason}",
            BranchId = original.BranchId,
            SourceType = JournalEntrySourceType.Reversal,
            SourceId = original.Id,
            Postings = original.Postings.Select(p => new CreateJournalPostingRequest
            {
                AccountId = p.AccountId,
                DebitAmount = p.CreditAmount,   // Swap: original credit becomes reversal debit
                CreditAmount = p.DebitAmount,   // Swap: original debit becomes reversal credit
                Memo = $"Reversal of {original.EntryNumber}"
            }).ToList()
        };

        var reversal = await PostJournalEntryAsync(tenantId, userId, reversalRequest);

        // Mark original as reversed
        original.IsReversed = true;
        original.ReversedByEntryId = reversal.Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Journal entry {OriginalNumber} reversed by {ReversalNumber} in Tenant {TenantId}",
            original.EntryNumber, reversal.EntryNumber, tenantId);

        return reversal;
    }

    // ── Query Methods ──────────────────────────────────────────────────────

    public async Task<(List<JournalEntryDto> Entries, int TotalCount)> GetJournalEntriesAsync(
        int tenantId, int? branchId, int? fiscalPeriodId,
        JournalEntrySourceType? sourceType, DateTime? dateFrom, DateTime? dateTo,
        int pageNumber, int pageSize)
    {
        var query = _context.JournalEntries
            .Include(e => e.Postings).ThenInclude(p => p.Account)
            .Include(e => e.Branch)
            .Where(e => e.TenantId == tenantId);

        if (branchId.HasValue)
            query = query.Where(e => e.BranchId == branchId.Value);
        if (fiscalPeriodId.HasValue)
            query = query.Where(e => e.FiscalPeriodId == fiscalPeriodId.Value);
        if (sourceType.HasValue)
            query = query.Where(e => e.SourceType == sourceType.Value);
        if (dateFrom.HasValue)
            query = query.Where(e => e.EntryDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(e => e.EntryDate <= dateTo.Value);

        var totalCount = await query.CountAsync();
        var entries = await query
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.Id)
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Min(pageSize, 100))
            .Take(Math.Min(pageSize, 100))
            .ToListAsync();

        var dtos = new List<JournalEntryDto>();
        foreach (var entry in entries)
            dtos.Add(await MapToJournalEntryDtoAsync(entry));

        return (dtos, totalCount);
    }

    public async Task<JournalEntryDto?> GetJournalEntryByIdAsync(int tenantId, int journalEntryId)
    {
        var entry = await _context.JournalEntries
            .Include(e => e.Postings).ThenInclude(p => p.Account)
            .Include(e => e.Branch)
            .FirstOrDefaultAsync(e => e.Id == journalEntryId && e.TenantId == tenantId);

        return entry is null ? null : await MapToJournalEntryDtoAsync(entry);
    }

    // ── Financial Reports ──────────────────────────────────────────────────

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(int tenantId, int fiscalPeriodId)
    {
        var period = await _context.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == fiscalPeriodId && p.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Fiscal period {fiscalPeriodId} not found.");

        var balances = await _context.AccountBalances
            .Include(ab => ab.Account)
            .Where(ab => ab.TenantId == tenantId && ab.FiscalPeriodId == fiscalPeriodId)
            .OrderBy(ab => ab.Account.Code)
            .ToListAsync();

        var lines = balances
            .Where(ab => ab.ClosingDebit != 0 || ab.ClosingCredit != 0)
            .Select(ab => new TrialBalanceLineDto
            {
                AccountId = ab.AccountId,
                AccountCode = ab.Account.Code,
                AccountName = ab.Account.Name,
                AccountType = ab.Account.AccountType.ToString(),
                DebitBalance = ab.ClosingDebit,
                CreditBalance = ab.ClosingCredit
            }).ToList();

        var totalDebit = lines.Sum(l => l.DebitBalance);
        var totalCredit = lines.Sum(l => l.CreditBalance);

        return new TrialBalanceDto
        {
            FiscalPeriodId = fiscalPeriodId,
            PeriodLabel = $"Period {period.PeriodNumber} — {period.FiscalYear.Year}",
            PeriodStart = period.StartDate,
            PeriodEnd = period.EndDate,
            Lines = lines,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            IsBalanced = Math.Abs(totalDebit - totalCredit) < 0.01m
        };
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(int tenantId, DateTime asOfDate)
    {
        // Find the fiscal period containing asOfDate (or the latest closed one before it)
        var period = await FindFiscalPeriodForDateAsync(tenantId, asOfDate);
        if (period == null)
        {
            // Fallback: get the latest period
            period = await _context.FiscalPeriods
                .Where(p => p.TenantId == tenantId && p.EndDate <= asOfDate)
                .OrderByDescending(p => p.EndDate)
                .FirstOrDefaultAsync();
        }

        var accounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync();

        // Get balances — either from materialized table or compute from postings
        Dictionary<int, decimal> accountBalances;
        if (period != null)
        {
            var balanceRows = await _context.AccountBalances
                .Where(ab => ab.TenantId == tenantId && ab.FiscalPeriodId == period.Id)
                .ToListAsync();
            accountBalances = balanceRows.ToDictionary(
                ab => ab.AccountId,
                ab => ab.ClosingDebit - ab.ClosingCredit);
        }
        else
        {
            accountBalances = new Dictionary<int, decimal>();
        }

        decimal GetBalance(Account a)
        {
            accountBalances.TryGetValue(a.Id, out var bal);
            // Assets have debit-normal → positive balance = debit > credit
            // Liabilities/Equity have credit-normal → flip sign for display
            return a.NormalBalance == NormalBalance.Debit ? bal : -bal;
        }

        var assetAccounts = accounts.Where(a => a.AccountType == AccountType.Asset).ToList();
        var liabilityAccounts = accounts.Where(a => a.AccountType == AccountType.Liability).ToList();
        var equityAccounts = accounts.Where(a => a.AccountType == AccountType.Equity).ToList();

        var assets = BuildBalanceSheetSection("Assets", assetAccounts, GetBalance);
        var liabilities = BuildBalanceSheetSection("Liabilities", liabilityAccounts, GetBalance);
        var equity = BuildBalanceSheetSection("Equity", equityAccounts, GetBalance);

        return new BalanceSheetDto
        {
            AsOfDate = asOfDate,
            Assets = assets,
            Liabilities = liabilities,
            Equity = equity,
            TotalAssets = assets.Total,
            TotalLiabilitiesAndEquity = liabilities.Total + equity.Total,
            IsBalanced = Math.Abs(assets.Total - (liabilities.Total + equity.Total)) < 0.01m
        };
    }

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(
        int tenantId, DateTime periodStart, DateTime periodEnd)
    {
        // Sum postings within the date range for Revenue, ContraRevenue, and Expense accounts
        var postings = await _context.JournalPostings
            .Include(p => p.JournalEntry)
            .Include(p => p.Account)
            .Where(p => p.JournalEntry.TenantId == tenantId
                     && p.JournalEntry.IsPosted
                     && p.JournalEntry.EntryDate >= periodStart
                     && p.JournalEntry.EntryDate <= periodEnd)
            .ToListAsync();

        var accountTotals = postings
            .GroupBy(p => p.AccountId)
            .Select(g => new
            {
                Account = g.First().Account,
                TotalDebit = g.Sum(p => p.DebitAmount),
                TotalCredit = g.Sum(p => p.CreditAmount)
            })
            .ToList();

        var revenueLines = accountTotals
            .Where(x => x.Account.AccountType == AccountType.Revenue)
            .Select(x => new IncomeStatementLineDto
            {
                AccountId = x.Account.Id,
                AccountCode = x.Account.Code,
                AccountName = x.Account.Name,
                Amount = x.TotalCredit - x.TotalDebit // Revenue: credit-normal
            })
            .OrderBy(x => x.AccountCode)
            .ToList();

        var contraRevenueLines = accountTotals
            .Where(x => x.Account.AccountType == AccountType.ContraRevenue)
            .Select(x => new IncomeStatementLineDto
            {
                AccountId = x.Account.Id,
                AccountCode = x.Account.Code,
                AccountName = x.Account.Name,
                Amount = x.TotalDebit - x.TotalCredit // ContraRevenue: debit-normal
            })
            .OrderBy(x => x.AccountCode)
            .ToList();

        var expenseLines = accountTotals
            .Where(x => x.Account.AccountType == AccountType.Expense)
            .Select(x => new IncomeStatementLineDto
            {
                AccountId = x.Account.Id,
                AccountCode = x.Account.Code,
                AccountName = x.Account.Name,
                Amount = x.TotalDebit - x.TotalCredit // Expense: debit-normal
            })
            .OrderBy(x => x.AccountCode)
            .ToList();

        var grossRevenue = revenueLines.Sum(l => l.Amount);
        var totalContraRevenue = contraRevenueLines.Sum(l => l.Amount);
        var netRevenue = grossRevenue - totalContraRevenue;
        var totalExpenses = expenseLines.Sum(l => l.Amount);
        var netIncome = netRevenue - totalExpenses;

        return new IncomeStatementDto
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Revenue = new IncomeStatementSectionDto
            {
                SectionName = "Gross Patient Service Revenue",
                Total = grossRevenue,
                Lines = revenueLines
            },
            ContraRevenue = new IncomeStatementSectionDto
            {
                SectionName = "Revenue Deductions",
                Total = totalContraRevenue,
                Lines = contraRevenueLines
            },
            NetRevenue = netRevenue,
            Expenses = new IncomeStatementSectionDto
            {
                SectionName = "Operating Expenses",
                Total = totalExpenses,
                Lines = expenseLines
            },
            NetIncome = netIncome,
            NetMarginPercent = netRevenue > 0 ? (netIncome / netRevenue) * 100 : 0
        };
    }

    // ── Chart of Accounts ──────────────────────────────────────────────────

    public async Task<List<AccountDto>> GetChartOfAccountsAsync(int tenantId)
    {
        var accounts = await _context.Accounts
            .Include(a => a.CostCenter)
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Code)
            .ToListAsync();

        // Get current balances (from the latest open period)
        var latestPeriod = await _context.FiscalPeriods
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.EndDate)
            .FirstOrDefaultAsync();

        var balanceMap = new Dictionary<int, decimal>();
        if (latestPeriod != null)
        {
            var balances = await _context.AccountBalances
                .Where(ab => ab.TenantId == tenantId && ab.FiscalPeriodId == latestPeriod.Id)
                .ToListAsync();
            balanceMap = balances.ToDictionary(
                ab => ab.AccountId,
                ab => ab.ClosingDebit - ab.ClosingCredit);
        }

        // Build tree
        var allDtos = accounts.Select(a => MapToAccountDto(a, balanceMap)).ToList();
        var lookup = allDtos.ToDictionary(d => d.Id);

        var roots = new List<AccountDto>();
        foreach (var dto in allDtos)
        {
            if (dto.ParentAccountId.HasValue && lookup.TryGetValue(dto.ParentAccountId.Value, out var parent))
                parent.Children.Add(dto);
            else
                roots.Add(dto);
        }

        return roots;
    }

    public async Task<AccountDto> CreateAccountAsync(int tenantId, CreateAccountRequest request)
    {
        // Validate code uniqueness
        var exists = await _context.Accounts
            .AnyAsync(a => a.TenantId == tenantId && a.Code == request.Code);
        if (exists)
            throw new ArgumentException($"Account code '{request.Code}' already exists.");

        // Validate parent if specified
        if (request.ParentAccountId.HasValue)
        {
            var parent = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.ParentAccountId.Value && a.TenantId == tenantId);
            if (parent == null)
                throw new ArgumentException("Parent account not found.");
        }

        var normalBalance = request.AccountType switch
        {
            AccountType.Asset or AccountType.Expense or AccountType.ContraRevenue => NormalBalance.Debit,
            _ => NormalBalance.Credit
        };

        var account = new Account
        {
            TenantId = tenantId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            AccountType = request.AccountType,
            NormalBalance = normalBalance,
            ParentAccountId = request.ParentAccountId,
            CostCenterId = request.CostCenterId,
            Description = request.Description?.Trim(),
            IsSystemAccount = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Account {Code} '{Name}' created for Tenant {TenantId}",
            account.Code, account.Name, tenantId);

        return MapToAccountDto(account, new Dictionary<int, decimal>());
    }

    public async Task<AccountDto> UpdateAccountAsync(int tenantId, int accountId, UpdateAccountRequest request)
    {
        var account = await _context.Accounts
            .Include(a => a.CostCenter)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        if (request.Name != null) account.Name = request.Name.Trim();
        if (request.Description != null) account.Description = request.Description.Trim();
        if (request.CostCenterId.HasValue) account.CostCenterId = request.CostCenterId.Value;
        if (request.IsActive.HasValue) account.IsActive = request.IsActive.Value;

        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToAccountDto(account, new Dictionary<int, decimal>());
    }

    // ── Fiscal Periods ─────────────────────────────────────────────────────

    public async Task<List<FiscalPeriodDto>> GetFiscalPeriodsAsync(int tenantId)
    {
        var periods = await _context.FiscalPeriods
            .Include(p => p.FiscalYear)
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        return periods.Select(p => new FiscalPeriodDto
        {
            Id = p.Id,
            FiscalYearId = p.FiscalYearId,
            Year = p.FiscalYear.Year,
            PeriodNumber = p.PeriodNumber,
            Label = $"Period {p.PeriodNumber} — FY {p.FiscalYear.Year}",
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status.ToString(),
            ClosedAt = p.ClosedAt
        }).ToList();
    }

    public async Task<FiscalPeriod?> FindFiscalPeriodForDateAsync(int tenantId, DateTime date)
    {
        return await _context.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId
                                   && p.StartDate <= date
                                   && p.EndDate >= date);
    }

    public async Task CloseFiscalPeriodAsync(int tenantId, int fiscalPeriodId, int userId)
    {
        var period = await _context.FiscalPeriods
            .FirstOrDefaultAsync(p => p.Id == fiscalPeriodId && p.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Fiscal period {fiscalPeriodId} not found.");

        if (period.Status == FiscalPeriodStatus.Locked)
            throw new InvalidOperationException("This period is permanently locked.");

        period.Status = FiscalPeriodStatus.Closed;
        period.ClosedByUserId = userId;
        period.ClosedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Fiscal period {PeriodId} closed for Tenant {TenantId}", fiscalPeriodId, tenantId);
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private async Task<string> GenerateEntryNumberAsync(int tenantId, DateTime entryDate)
    {
        var prefix = $"JE-{entryDate:yyyyMMdd}-";
        var maxNumber = await _context.JournalEntries
            .Where(e => e.TenantId == tenantId && e.EntryNumber.StartsWith(prefix))
            .OrderByDescending(e => e.EntryNumber)
            .Select(e => e.EntryNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (maxNumber != null && int.TryParse(maxNumber[prefix.Length..], out var current))
            nextSeq = current + 1;

        return $"{prefix}{nextSeq:D4}";
    }

    private async Task UpdateAccountBalancesAsync(
        int tenantId, int fiscalPeriodId, ICollection<JournalPosting> postings)
    {
        foreach (var posting in postings)
        {
            var balance = await _context.AccountBalances
                .FirstOrDefaultAsync(ab =>
                    ab.TenantId == tenantId
                    && ab.AccountId == posting.AccountId
                    && ab.FiscalPeriodId == fiscalPeriodId);

            if (balance == null)
            {
                balance = new AccountBalance
                {
                    TenantId = tenantId,
                    AccountId = posting.AccountId,
                    FiscalPeriodId = fiscalPeriodId
                };
                _context.AccountBalances.Add(balance);
            }

            balance.PeriodDebit += posting.DebitAmount;
            balance.PeriodCredit += posting.CreditAmount;
            balance.ClosingDebit = balance.OpeningDebit + balance.PeriodDebit;
            balance.ClosingCredit = balance.OpeningCredit + balance.PeriodCredit;
            balance.LastUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<JournalEntryDto> MapToJournalEntryDtoAsync(JournalEntry entry)
    {
        // Load postings if not already loaded
        if (!entry.Postings.Any())
        {
            await _context.Entry(entry)
                .Collection(e => e.Postings)
                .Query()
                .Include(p => p.Account)
                .LoadAsync();
        }

        var creator = await _context.Users
            .Where(u => u.Id == entry.CreatedByUserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        var period = await _context.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == entry.FiscalPeriodId);

        return new JournalEntryDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            EntryDate = entry.EntryDate,
            Description = entry.Description,
            SourceType = entry.SourceType.ToString(),
            SourceId = entry.SourceId,
            IsReversed = entry.IsReversed,
            ReversedByEntryId = entry.ReversedByEntryId,
            IsPosted = entry.IsPosted,
            BranchId = entry.BranchId,
            BranchName = entry.Branch?.Name,
            FiscalPeriodId = entry.FiscalPeriodId,
            FiscalPeriodLabel = period != null
                ? $"Period {period.PeriodNumber} — FY {period.FiscalYear.Year}"
                : null,
            TotalDebit = entry.Postings.Sum(p => p.DebitAmount),
            TotalCredit = entry.Postings.Sum(p => p.CreditAmount),
            CreatedByName = creator,
            CreatedAt = entry.CreatedAt,
            Postings = entry.Postings.Select(p => new JournalPostingDto
            {
                Id = p.Id,
                AccountId = p.AccountId,
                AccountCode = p.Account?.Code ?? "",
                AccountName = p.Account?.Name ?? "",
                DebitAmount = p.DebitAmount,
                CreditAmount = p.CreditAmount,
                Memo = p.Memo
            }).ToList()
        };
    }

    private static AccountDto MapToAccountDto(Account a, Dictionary<int, decimal> balanceMap)
    {
        balanceMap.TryGetValue(a.Id, out var rawBalance);
        // For display: Assets/Expenses show debit-positive; Liabilities/Equity/Revenue show credit-positive
        var displayBalance = a.NormalBalance == NormalBalance.Debit ? rawBalance : -rawBalance;

        return new AccountDto
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            AccountType = a.AccountType.ToString(),
            NormalBalance = a.NormalBalance.ToString(),
            ParentAccountId = a.ParentAccountId,
            IsSystemAccount = a.IsSystemAccount,
            CostCenterId = a.CostCenterId,
            CostCenterName = a.CostCenter?.Name,
            IsActive = a.IsActive,
            Description = a.Description,
            CurrentBalance = displayBalance
        };
    }

    private static BalanceSheetSectionDto BuildBalanceSheetSection(
        string sectionName, List<Account> accounts, Func<Account, decimal> getBalance)
    {
        var rootAccounts = accounts.Where(a => a.ParentAccountId == null ||
            !accounts.Any(pa => pa.Id == a.ParentAccountId)).ToList();

        var lines = rootAccounts.Select(a => BuildBalanceSheetLine(a, accounts, getBalance)).ToList();

        return new BalanceSheetSectionDto
        {
            SectionName = sectionName,
            Total = lines.Sum(l => l.Balance),
            Lines = lines
        };
    }

    private static BalanceSheetLineDto BuildBalanceSheetLine(
        Account account, List<Account> allAccounts, Func<Account, decimal> getBalance)
    {
        var children = allAccounts
            .Where(a => a.ParentAccountId == account.Id)
            .Select(a => BuildBalanceSheetLine(a, allAccounts, getBalance))
            .ToList();

        var balance = children.Any()
            ? children.Sum(c => c.Balance)
            : getBalance(account);

        return new BalanceSheetLineDto
        {
            AccountId = account.Id,
            AccountCode = account.Code,
            AccountName = account.Name,
            Balance = balance,
            Children = children
        };
    }
}
