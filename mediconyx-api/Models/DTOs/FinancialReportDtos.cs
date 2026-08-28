namespace mediconyx_api.Models.DTOs;

/// <summary>Financial reporting DTOs.</summary>

public class TrialBalanceDto
{
    public int FiscalPeriodId { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
}

public class TrialBalanceLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
}

public class BalanceSheetDto
{
    public DateTime AsOfDate { get; set; }
    public BalanceSheetSectionDto Assets { get; set; } = new();
    public BalanceSheetSectionDto Liabilities { get; set; } = new();
    public BalanceSheetSectionDto Equity { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilitiesAndEquity { get; set; }
    public bool IsBalanced { get; set; }
}

public class BalanceSheetSectionDto
{
    public string SectionName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<BalanceSheetLineDto> Lines { get; set; } = new();
}

public class BalanceSheetLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public List<BalanceSheetLineDto> Children { get; set; } = new();
}

public class IncomeStatementDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public IncomeStatementSectionDto Revenue { get; set; } = new();
    public IncomeStatementSectionDto ContraRevenue { get; set; } = new();
    public decimal NetRevenue { get; set; }
    public IncomeStatementSectionDto Expenses { get; set; } = new();
    public decimal NetIncome { get; set; }
    public decimal NetMarginPercent { get; set; }
}

public class IncomeStatementSectionDto
{
    public string SectionName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<IncomeStatementLineDto> Lines { get; set; } = new();
}

public class IncomeStatementLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
