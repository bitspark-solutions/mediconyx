namespace mediconyx_api.Models;

/// <summary>
/// Classification of accounts in the Chart of Accounts.
/// Code ranges follow standard accounting conventions:
///   1xxx = Asset, 2xxx = Liability, 3xxx = Equity,
///   4xxx = Revenue, 49xx = Contra-Revenue, 5xxx = Expense.
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    ContraRevenue = 5,
    Expense = 6
}
