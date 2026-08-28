using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Accounting;

/// <summary>
/// Generates double-entry journal entries automatically when invoice lifecycle
/// events occur (issued, payment, refund). Links invoices to the General Ledger.
/// </summary>
public class InvoiceAccountingService : IInvoiceAccountingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<InvoiceAccountingService> _logger;

    // Well-known account codes (must match AccountSeeder)
    private const string CashOnHand = "1010";
    private const string BankAccount = "1020";
    private const string ArPatient = "1030";
    private const string RefundsPayable = "2050";
    private const string RevenueBedCharges = "4010";
    private const string RevenueConsultation = "4020";
    private const string RevenuePharmacy = "4030";
    private const string RevenueLab = "4040";
    private const string RevenueSurgery = "4050";
    private const string RevenueEmergency = "4060";
    private const string RevenueMaternity = "4070";
    private const string RevenueICU = "4080";
    private const string MiscRevenue = "4520";

    public InvoiceAccountingService(
        ApplicationDbContext context,
        ILedgerService ledgerService,
        ILogger<InvoiceAccountingService> logger)
    {
        _context = context;
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task OnInvoiceIssuedAsync(int tenantId, int invoiceId, int userId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        // Get structured line items if available
        var lineItems = await _context.InvoiceLineItems
            .Where(li => li.InvoiceId == invoiceId && li.TenantId == tenantId)
            .ToListAsync();

        var arAccount = await GetAccountByCodeAsync(tenantId, ArPatient);

        var postings = new List<CreateJournalPostingRequest>
        {
            // Debit: Accounts Receivable for the total invoice amount
            new()
            {
                AccountId = arAccount.Id,
                DebitAmount = invoice.TotalAmount,
                CreditAmount = 0,
                Memo = $"AR for invoice {invoice.InvoiceNumber}"
            }
        };

        if (lineItems.Any())
        {
            // Credit each revenue account per line item
            foreach (var item in lineItems)
            {
                var revenueAccountId = item.RevenueAccountId
                    ?? (await ResolveRevenueAccountAsync(tenantId, item.ServiceType)).Id;

                postings.Add(new CreateJournalPostingRequest
                {
                    AccountId = revenueAccountId,
                    DebitAmount = 0,
                    CreditAmount = item.NetAmount,
                    Memo = item.Description
                });
            }
        }
        else
        {
            // Fallback: single revenue credit using Misc Revenue
            var miscAccount = await GetAccountByCodeAsync(tenantId, MiscRevenue);
            postings.Add(new CreateJournalPostingRequest
            {
                AccountId = miscAccount.Id,
                DebitAmount = 0,
                CreditAmount = invoice.TotalAmount,
                Memo = $"Revenue for invoice {invoice.InvoiceNumber}"
            });
        }

        await _ledgerService.PostJournalEntryAsync(tenantId, userId, new CreateJournalEntryRequest
        {
            EntryDate = invoice.CreatedAt,
            Description = $"Invoice issued: {invoice.InvoiceNumber}",
            BranchId = invoice.BranchId,
            SourceType = JournalEntrySourceType.InvoiceIssued,
            SourceId = invoiceId,
            Postings = postings
        });

        _logger.LogInformation("Accounting entry generated for invoice {InvoiceNumber} in Tenant {TenantId}",
            invoice.InvoiceNumber, tenantId);
    }

    public async Task OnPaymentReceivedAsync(
        int tenantId, int invoiceId, decimal amount,
        InvoicePaymentMethod paymentMethod, int userId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        // Determine cash account based on payment method
        var cashAccountCode = paymentMethod switch
        {
            InvoicePaymentMethod.Cash => CashOnHand,
            _ => BankAccount // Card, BankTransfer, bKash, Nagad, Rocket all go to bank
        };

        var cashAccount = await GetAccountByCodeAsync(tenantId, cashAccountCode);
        var arAccount = await GetAccountByCodeAsync(tenantId, ArPatient);

        await _ledgerService.PostJournalEntryAsync(tenantId, userId, new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow,
            Description = $"Payment received for {invoice.InvoiceNumber} via {paymentMethod}",
            BranchId = invoice.BranchId,
            SourceType = JournalEntrySourceType.InvoicePayment,
            SourceId = invoiceId,
            Postings = new List<CreateJournalPostingRequest>
            {
                new() { AccountId = cashAccount.Id, DebitAmount = amount, CreditAmount = 0,
                         Memo = $"Cash receipt — {paymentMethod}" },
                new() { AccountId = arAccount.Id, DebitAmount = 0, CreditAmount = amount,
                         Memo = $"AR reduction — {invoice.InvoiceNumber}" }
            }
        });
    }

    public async Task OnRefundIssuedAsync(int tenantId, int invoiceId, decimal amount, int userId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        var refundsPayable = await GetAccountByCodeAsync(tenantId, RefundsPayable);
        var cashAccount = await GetAccountByCodeAsync(tenantId, CashOnHand);

        await _ledgerService.PostJournalEntryAsync(tenantId, userId, new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow,
            Description = $"Refund issued for {invoice.InvoiceNumber}",
            BranchId = invoice.BranchId,
            SourceType = JournalEntrySourceType.Refund,
            SourceId = invoiceId,
            Postings = new List<CreateJournalPostingRequest>
            {
                new() { AccountId = refundsPayable.Id, DebitAmount = amount, CreditAmount = 0,
                         Memo = "Patient refund obligation" },
                new() { AccountId = cashAccount.Id, DebitAmount = 0, CreditAmount = amount,
                         Memo = "Cash disbursement — refund" }
            }
        });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Account> GetAccountByCodeAsync(int tenantId, string code)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code == code && a.IsActive)
            ?? throw new InvalidOperationException(
                $"System account '{code}' not found for tenant {tenantId}. " +
                "Please ensure the Chart of Accounts has been seeded.");
    }

    private async Task<Account> ResolveRevenueAccountAsync(int tenantId, string serviceType)
    {
        var code = serviceType switch
        {
            "BedCharge" => RevenueBedCharges,
            "Consultation" => RevenueConsultation,
            "Pharmacy" => RevenuePharmacy,
            "Lab" or "Radiology" => RevenueLab,
            "Surgery" => RevenueSurgery,
            "Emergency" => RevenueEmergency,
            "Maternity" => RevenueMaternity,
            "ICU" or "PICU" or "NICU" => RevenueICU,
            _ => MiscRevenue
        };

        return await GetAccountByCodeAsync(tenantId, code);
    }
}
