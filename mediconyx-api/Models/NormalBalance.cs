namespace mediconyx_api.Models;

/// <summary>
/// The "normal" side for an account type in double-entry bookkeeping.
/// Assets and Expenses increase with Debits.
/// Liabilities, Equity, and Revenue increase with Credits.
/// Contra-Revenue increases with Debits (it reduces Revenue).
/// </summary>
public enum NormalBalance
{
    Debit = 0,
    Credit = 1
}
