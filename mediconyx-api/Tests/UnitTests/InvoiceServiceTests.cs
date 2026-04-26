using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Invoices;

namespace mediconyx_api.Tests.UnitTests;

public class InvoiceServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly InvoiceService _sut;
    private const int TenantId = 1;

    public InvoiceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new InvoiceService(_context, Mock.Of<ILogger<InvoiceService>>());
    }

    private Invoice AddInvoice(int id, InvoiceStatus status = InvoiceStatus.Draft,
        decimal total = 500m, decimal paid = 0m, int patientId = 1)
    {
        var inv = new Invoice
        {
            Id = id, TenantId = TenantId, PatientId = patientId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{id:D4}",
            LineItems = "[]", SubTotal = total, TaxRate = 0, TaxAmount = 0,
            DiscountAmount = 0, TotalAmount = total, PaidAmount = paid,
            Status = status, CreatedAt = DateTime.UtcNow
        };
        _context.Invoices.Add(inv);
        _context.SaveChanges();
        return inv;
    }

    // ── GetInvoicesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoicesAsync_ShouldReturnOnlyTenantInvoices()
    {
        AddInvoice(1);
        _context.Invoices.Add(new Invoice
        {
            Id = 2, TenantId = 99, PatientId = 1,
            InvoiceNumber = "INV-OTHER-0001", LineItems = "[]",
            SubTotal = 100, TaxRate = 0, TaxAmount = 0, DiscountAmount = 0,
            TotalAmount = 100, PaidAmount = 0, Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetInvoicesAsync(TenantId,
            new InvoiceQueryParams { PageSize = 20, PageNumber = 1 });

        result.Invoices.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetInvoicesAsync_ShouldFilterByStatus()
    {
        AddInvoice(1, InvoiceStatus.Draft);
        AddInvoice(2, InvoiceStatus.Paid);

        var result = await _sut.GetInvoicesAsync(TenantId, new InvoiceQueryParams
        {
            Status = InvoiceStatus.Paid, PageSize = 20, PageNumber = 1
        });

        result.Invoices.Should().ContainSingle().Which.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task GetInvoicesAsync_ShouldPaginate()
    {
        for (int i = 1; i <= 6; i++) AddInvoice(i);

        var result = await _sut.GetInvoicesAsync(TenantId,
            new InvoiceQueryParams { PageSize = 4, PageNumber = 2 });

        result.Invoices.Should().HaveCount(2);
        result.TotalCount.Should().Be(6);
    }

    // ── GetInvoiceByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoiceByIdAsync_ShouldReturn_WhenFound()
    {
        AddInvoice(1);

        var result = await _sut.GetInvoiceByIdAsync(TenantId, invoiceId: 1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _sut.GetInvoiceByIdAsync(TenantId, invoiceId: 999);
        result.Should().BeNull();
    }

    // ── GetInvoicesByPatientAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetInvoicesByPatientAsync_ShouldReturnOnlyPatientInvoices()
    {
        AddInvoice(1, patientId: 1);
        AddInvoice(2, patientId: 2);
        AddInvoice(3, patientId: 1);

        var result = await _sut.GetInvoicesByPatientAsync(TenantId, patientId: 1, pageNumber: 1, pageSize: 20);

        result.Invoices.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    // ── GetRevenueStatsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRevenueStatsAsync_ShouldCalculateCorrectStats()
    {
        var today = DateTime.UtcNow;

        var paid = AddInvoice(1, InvoiceStatus.Paid, total: 300m, paid: 300m);
        paid.PaidAt = today;
        var partial = AddInvoice(2, InvoiceStatus.PartiallyPaid, total: 400m, paid: 100m);
        partial.PaidAt = today;
        var draft = AddInvoice(3, InvoiceStatus.Draft, total: 200m, paid: 0m);
        var overdue = AddInvoice(4, InvoiceStatus.Overdue, total: 150m, paid: 0m);
        await _context.SaveChangesAsync();

        var result = await _sut.GetRevenueStatsAsync(TenantId);

        result.TodayRevenue.Should().Be(400m);
        result.OutstandingAmount.Should().Be(650m);
        result.TotalInvoices.Should().Be(4);
        result.PaidCount.Should().Be(1);
        result.OverdueCount.Should().Be(1);
    }

    // ── CreateInvoiceAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateInvoiceAsync_ShouldPersistWithDraftStatus()
    {
        var request = new CreateInvoiceRequest
        {
            PatientId = 1, LineItems = "[]",
            SubTotal = 500m, TaxRate = 0, TaxAmount = 0,
            DiscountAmount = 0, TotalAmount = 500m,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        var result = await _sut.CreateInvoiceAsync(TenantId, createdByUserId: 1, request);

        result.Status.Should().Be("Draft");
        result.TotalAmount.Should().Be(500m);
        _context.Invoices.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldGenerateSequentialNumbers()
    {
        var request = new CreateInvoiceRequest
        {
            PatientId = 1, LineItems = "[]",
            SubTotal = 100m, TaxRate = 0, TaxAmount = 0,
            DiscountAmount = 0, TotalAmount = 100m
        };

        var first = await _sut.CreateInvoiceAsync(TenantId, 1, request);
        var second = await _sut.CreateInvoiceAsync(TenantId, 1, request);

        first.InvoiceNumber.Should().EndWith("0001");
        second.InvoiceNumber.Should().EndWith("0002");
    }

    // ── UpdateInvoiceAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateInvoiceAsync_ShouldUpdateAmounts_WhenDraft()
    {
        AddInvoice(1, InvoiceStatus.Draft, total: 500m);

        var request = new UpdateInvoiceRequest { TotalAmount = 750m };
        var result = await _sut.UpdateInvoiceAsync(TenantId, invoiceId: 1, request);

        result.TotalAmount.Should().Be(750m);
    }

    [Fact]
    public async Task UpdateInvoiceAsync_ShouldThrow_WhenNotDraft()
    {
        AddInvoice(1, InvoiceStatus.Issued);

        await _sut.Invoking(s => s.UpdateInvoiceAsync(TenantId, 1, new UpdateInvoiceRequest { TotalAmount = 100m }))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*draft*");
    }

    [Fact]
    public async Task UpdateInvoiceAsync_ShouldThrow_WhenNotFound()
    {
        await _sut.Invoking(s => s.UpdateInvoiceAsync(TenantId, 999, new UpdateInvoiceRequest()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateInvoiceStatusAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateInvoiceStatusAsync_ShouldSetDueDate_WhenIssuedWithoutOne()
    {
        AddInvoice(1, InvoiceStatus.Draft);

        var result = await _sut.UpdateInvoiceStatusAsync(TenantId, 1, InvoiceStatus.Issued);

        result.Status.Should().Be("Issued");
        var inv = await _context.Invoices.FindAsync(1);
        inv!.DueDate.Should().NotBeNull();
    }

    // ── RecordPaymentAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RecordPaymentAsync_ShouldSetPaidStatus_WhenFullyPaid()
    {
        AddInvoice(1, InvoiceStatus.Issued, total: 500m, paid: 0m);

        var result = await _sut.RecordPaymentAsync(TenantId, 1, new RecordPaymentRequest
        {
            Amount = 500m, PaymentMethod = InvoicePaymentMethod.Cash
        });

        result.Status.Should().Be("Paid");
        result.PaidAmount.Should().Be(500m);
        result.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public async Task RecordPaymentAsync_ShouldSetPartiallyPaidStatus_WhenPartialPayment()
    {
        AddInvoice(1, InvoiceStatus.Issued, total: 500m, paid: 0m);

        var result = await _sut.RecordPaymentAsync(TenantId, 1, new RecordPaymentRequest
        {
            Amount = 200m, PaymentMethod = InvoicePaymentMethod.Card
        });

        result.Status.Should().Be("PartiallyPaid");
        result.PaidAmount.Should().Be(200m);
        result.BalanceDue.Should().Be(300m);
    }

    [Fact]
    public async Task RecordPaymentAsync_ShouldThrow_WhenAmountExceedsBalance()
    {
        AddInvoice(1, InvoiceStatus.Issued, total: 500m, paid: 0m);

        await _sut.Invoking(s => s.RecordPaymentAsync(TenantId, 1, new RecordPaymentRequest
        {
            Amount = 600m, PaymentMethod = InvoicePaymentMethod.Cash
        })).Should().ThrowAsync<ArgumentException>().WithMessage("*exceeds balance*");
    }

    [Fact]
    public async Task RecordPaymentAsync_ShouldThrow_WhenAmountIsZero()
    {
        AddInvoice(1, InvoiceStatus.Issued, total: 500m);

        await _sut.Invoking(s => s.RecordPaymentAsync(TenantId, 1, new RecordPaymentRequest
        {
            Amount = 0m, PaymentMethod = InvoicePaymentMethod.Cash
        })).Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task RecordPaymentAsync_ShouldThrow_WhenInvoiceCancelled()
    {
        AddInvoice(1, InvoiceStatus.Cancelled, total: 500m);

        await _sut.Invoking(s => s.RecordPaymentAsync(TenantId, 1, new RecordPaymentRequest
        {
            Amount = 100m, PaymentMethod = InvoicePaymentMethod.Cash
        })).Should().ThrowAsync<InvalidOperationException>().WithMessage("*cancelled*");
    }

    // ── DeleteInvoiceAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteInvoiceAsync_ShouldRemove_WhenDraft()
    {
        AddInvoice(1, InvoiceStatus.Draft);

        await _sut.DeleteInvoiceAsync(TenantId, invoiceId: 1);

        _context.Invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteInvoiceAsync_ShouldThrow_WhenNotDraft()
    {
        AddInvoice(1, InvoiceStatus.Issued);

        await _sut.Invoking(s => s.DeleteInvoiceAsync(TenantId, 1))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*draft*");
    }

    [Fact]
    public async Task DeleteInvoiceAsync_ShouldThrow_WhenNotFound()
    {
        await _sut.Invoking(s => s.DeleteInvoiceAsync(TenantId, 999))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
