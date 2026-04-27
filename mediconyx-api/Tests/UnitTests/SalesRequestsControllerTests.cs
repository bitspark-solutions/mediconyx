using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using mediconyx_api.Controllers;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Infrastructure;
using System.Security.Claims;

namespace mediconyx_api.Tests.UnitTests;

public class SalesRequestsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<SalesRequestsController>> _loggerMock;
    private readonly Mock<ICaptchaService> _captchaMock;
    private readonly Mock<IFileValidationService> _fileValidationMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IMemoryCache _cache;
    private readonly SalesRequestsController _sut;

    public SalesRequestsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _loggerMock = new Mock<ILogger<SalesRequestsController>>();
        _captchaMock = new Mock<ICaptchaService>();
        _fileValidationMock = new Mock<IFileValidationService>();
        _emailMock = new Mock<IEmailService>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _sut = new SalesRequestsController(
            _context, _loggerMock.Object, _captchaMock.Object,
            _fileValidationMock.Object, _cache, _emailMock.Object);

        // Set up default HTTP context with admin user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "1"),
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private void SetAnonymousUser()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private void SetUserWithRole(string role, int userId = 1)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "Test"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // ── CreatePublic ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePublic_ShouldCreateSalesRequest_WhenCaptchaValid()
    {
        // Arrange
        SetAnonymousUser();
        _captchaMock.Setup(c => c.VerifyTokenAsync("valid_token", null)).ReturnsAsync(true);

        var request = new PublicSalesRequestCreate
        {
            HospitalName = "New Hospital",
            ContactPerson = "John Doe",
            Email = "john@hospital.com",
            Phone = "01700000000",
            Region = "Dhaka",
            Notes = "Test request",
            CaptchaToken = "valid_token",
            SubscriptionPlan = "Standard",
            BillingCycle = BillingCycle.Monthly,
            Currency = "BDT",
            QuotedPrice = 50000,
            Seats = 10
        };

        // Act
        var result = await _sut.CreatePublic(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().NotBeNull();

        // Verify database state
        _context.SalesRequests.Should().ContainSingle(r => r.Email == "john@hospital.com");
        var savedRequest = _context.SalesRequests.First();
        savedRequest.Status.Should().Be(SalesRequestStatus.Pending);
        savedRequest.HospitalName.Should().Be("New Hospital");
        
        _emailMock.Verify(e => e.SendNewRequestNotificationAsync(It.IsAny<int>(), "New Hospital", "john@hospital.com"), Times.Once);
    }

    [Fact]
    public async Task CreatePublic_ShouldReturnBadRequest_WhenCaptchaInvalid()
    {
        // Arrange
        SetAnonymousUser();
        _captchaMock.Setup(c => c.VerifyTokenAsync("invalid_token", null)).ReturnsAsync(false);

        var request = new PublicSalesRequestCreate
        {
            HospitalName = "New Hospital",
            ContactPerson = "John Doe",
            Email = "john@hospital.com",
            Phone = "01700000000",
            CaptchaToken = "invalid_token"
        };

        // Act
        var result = await _sut.CreatePublic(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _context.SalesRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePublic_ShouldReturnBadRequest_WhenHospitalNameInvalid()
    {
        // Arrange
        SetAnonymousUser();
        _captchaMock.Setup(c => c.VerifyTokenAsync(It.IsAny<string>(), null)).ReturnsAsync(true);

        var request = new PublicSalesRequestCreate
        {
            HospitalName = "", // Invalid
            ContactPerson = "John Doe",
            Email = "john@hospital.com",
            Phone = "01700000000",
            CaptchaToken = "valid_token"
        };

        // Act
        var result = await _sut.CreatePublic(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreatePublic_ShouldReturnBadRequest_WhenEmailInvalid()
    {
        // Arrange
        SetAnonymousUser();
        _captchaMock.Setup(c => c.VerifyTokenAsync(It.IsAny<string>(), null)).ReturnsAsync(true);

        var request = new PublicSalesRequestCreate
        {
            HospitalName = "New Hospital",
            ContactPerson = "John Doe",
            Email = "not-an-email",
            Phone = "01700000000",
            CaptchaToken = "valid_token"
        };

        // Act
        var result = await _sut.CreatePublic(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── List ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ShouldReturnSalesRequests_WithFilters()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        _context.SalesRequests.AddRange(
            new SalesRequest { Id = 1, HospitalName = "Hospital A", Status = SalesRequestStatus.Pending, Region = "Dhaka", CreatedAt = DateTime.UtcNow },
            new SalesRequest { Id = 2, HospitalName = "Hospital B", Status = SalesRequestStatus.Active, Region = "Chittagong", CreatedAt = DateTime.UtcNow },
            new SalesRequest { Id = 3, HospitalName = "Hospital C", Status = SalesRequestStatus.Pending, Region = "Dhaka", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.List(new SalesRequestQuery { Status = SalesRequestStatus.Pending, Region = "Dhaka", Page = 1, PageSize = 20 });

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var itemsProperty = okResult.Value!.GetType().GetProperty("items");
        itemsProperty.Should().NotBeNull();
        var response = itemsProperty!.GetValue(okResult.Value) as System.Collections.IEnumerable;
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task List_ShouldSearchByQuery()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        _context.SalesRequests.AddRange(
            new SalesRequest { Id = 1, HospitalName = "Alpha Hospital", Email = "alpha@test.com", Status = SalesRequestStatus.Pending, CreatedAt = DateTime.UtcNow },
            new SalesRequest { Id = 2, HospitalName = "Beta Clinic", Email = "beta@test.com", Status = SalesRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.List(new SalesRequestQuery { Q = "alpha", Page = 1, PageSize = 20 });

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
    }

    // ── Detail ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ShouldReturnSalesRequest_WithIncludes()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        _context.SalesRequestAttachments.Add(new SalesRequestAttachment
        {
            Id = 1,
            SalesRequestId = 1,
            FileName = "doc.pdf",
            MimeType = "application/pdf",
            SizeBytes = 1024,
            StoragePath = "/uploads/1/doc.pdf",
            Sha256 = "hash123",
            UploadedAt = DateTime.UtcNow
        });
        _context.SalesRequestNotes.Add(new SalesRequestNote
        {
            Id = 1,
            SalesRequestId = 1,
            AuthorUserId = 1,
            Note = "Test note",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Detail(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
    }

    [Fact]
    public async Task Detail_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        SetUserWithRole("Sales");

        // Act
        var result = await _sut.Detail(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── Claim ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Claim_ShouldAssignOwnerToSalesRequest()
    {
        // Arrange
        SetUserWithRole("Sales", userId: 5);
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Claim(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.OwnerSalesUserId.Should().Be(5);
        updatedRequest.ClaimedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Claim_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        SetUserWithRole("Sales", userId: 5);

        // Act
        var result = await _sut.Claim(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Claim_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        SetUserWithRole("Sales", userId: 5);
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(); // No user

        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Claim(1);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ── Unassign ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unassign_ShouldRemoveOwnerFromSalesRequest()
    {
        // Arrange
        SetUserWithRole("Master");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            OwnerSalesUserId = 5,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Unassign(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.OwnerSalesUserId.Should().BeNull();
    }

    // ── Qualify ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Qualify_ShouldChangeStatus_FromPendingToQualified()
    {
        // Arrange
        SetUserWithRole("Sales", userId: 1);
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            Email = "test@hospital.com",
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Qualify(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.Status.Should().Be(SalesRequestStatus.Qualified);
        
        // Verify timeline event created
        var timelineEvent = await _context.OnboardingTimelineEvents.FirstOrDefaultAsync(e => e.SalesRequestId == 1);
        timelineEvent.Should().NotBeNull();
        timelineEvent!.Type.Should().Be("StatusChanged");
    }

    [Fact]
    public async Task Qualify_ShouldReturnBadRequest_WhenRequestNotPending()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Active, // Not Pending
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Qualify(1);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Qualify_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        SetUserWithRole("Sales");

        // Act
        var result = await _sut.Qualify(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── UpdateStatus ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ShouldChangeStatus_AndCreateAuditLog()
    {
        // Arrange
        SetUserWithRole("Sales", userId: 1);
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Qualified,
            Email = "test@hospital.com",
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.UpdateStatus(1, new SalesStatusUpdate { Status = SalesRequestStatus.InProgress, Reason = "Moving forward" });

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.Status.Should().Be(SalesRequestStatus.InProgress);
        
        // Verify audit log created
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == "1" && a.Action == "status_update");
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_ShouldSetActivatedAt_WhenStatusChangedToActive()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.AwaitingPayment,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.UpdateStatus(1, new SalesStatusUpdate { Status = SalesRequestStatus.Active });

        // Assert
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.Status.Should().Be(SalesRequestStatus.Active);
        updatedRequest.ActivatedAt.Should().NotBeNull();
    }

    // ── UpdatePaymentStatus ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePaymentStatus_ShouldSetPaidAt_WhenStatusPaid()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.UpdatePaymentStatus(1, new PaymentStatusUpdate { PaymentStatus = PaymentStatus.Paid });

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        updatedRequest.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePaymentStatus_ShouldNotClearPaidAt_WhenStatusChangedFromPaid()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        var paidAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            PaymentStatus = PaymentStatus.Paid,
            PaidAt = paidAt,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.UpdatePaymentStatus(1, new PaymentStatusUpdate { PaymentStatus = PaymentStatus.Pending });

        // Assert
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.PaymentStatus.Should().Be(PaymentStatus.Pending);
        updatedRequest.PaidAt.Should().Be(paidAt); // Should preserve original paid date
    }

    // ── Reject ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reject_ShouldChangeStatus_ToRejected_WithReason()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            Email = "test@hospital.com",
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Reject(1, new StatusActionRequest { Reason = "Incomplete documentation" });

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.Status.Should().Be(SalesRequestStatus.Rejected);
        updatedRequest.DeactivatedAt.Should().NotBeNull();
        
        // Verify timeline event
        var timelineEvent = await _context.OnboardingTimelineEvents.FirstOrDefaultAsync(e => e.SalesRequestId == 1 && e.Type == "Rejected");
        timelineEvent.Should().NotBeNull();
        timelineEvent!.Description.Should().Contain("Incomplete documentation");
    }

    [Fact]
    public async Task Reject_ShouldReturnBadRequest_WhenReasonMissing()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Reject(1, new StatusActionRequest { Reason = "" });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Reject_ShouldReturnBadRequest_WhenAlreadyRejected()
    {
        // Arrange
        SetUserWithRole("Sales");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Rejected,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Reject(1, new StatusActionRequest { Reason = "Duplicate rejection" });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Suspend ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Suspend_ShouldChangeStatus_FromActiveToSuspended()
    {
        // Arrange
        SetUserWithRole("Admin");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Active,
            Email = "test@hospital.com",
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Suspend(1, new StatusActionRequest { Reason = "Payment overdue" });

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.Status.Should().Be(SalesRequestStatus.Suspended);
    }

    [Fact]
    public async Task Suspend_ShouldReturnBadRequest_WhenRequestNotActive()
    {
        // Arrange
        SetUserWithRole("Admin");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending, // Not Active
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Suspend(1, new StatusActionRequest { Reason = "Should fail" });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Reactivate ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Reactivate_ShouldChangeStatus_FromSuspendedToActive()
    {
        // Arrange
        SetUserWithRole("Admin");
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Suspended,
            DeactivatedAt = DateTime.UtcNow.AddDays(-7),
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Reactivate(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.Status.Should().Be(SalesRequestStatus.Active);
        updatedRequest.DeactivatedAt.Should().BeNull();
    }

    // ── GetPendingProvisioningRequests ───────────────────────────────────────

    [Fact]
    public async Task GetPendingProvisioningRequests_ShouldReturnRequests_AwaitingPaymentWithPaidStatus()
    {
        // Arrange
        SetUserWithRole("Admin");
        
        _context.SalesRequests.AddRange(
            new SalesRequest
            {
                Id = 1,
                HospitalName = "Ready Hospital",
                Status = SalesRequestStatus.AwaitingPayment,
                PaymentStatus = PaymentStatus.Paid,
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new SalesRequest
            {
                Id = 2,
                HospitalName = "Not Ready Hospital",
                Status = SalesRequestStatus.AwaitingPayment,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new SalesRequest
            {
                Id = 3,
                HospitalName = "Wrong Status Hospital",
                Status = SalesRequestStatus.InProgress,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPendingProvisioningRequests();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        
        // The result should be an IEnumerable of anonymous objects
        var items = okResult.Value as System.Collections.IEnumerable;
        items.Should().NotBeNull();
        items.Cast<object>().Should().HaveCount(1); // Only "Ready Hospital" should match
    }

    // ── AddNote ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddNote_ShouldCreateNote_ForSalesRequest()
    {
        // Arrange
        SetUserWithRole("Sales", userId: 5);
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.AddNote(1, "Follow-up call scheduled for tomorrow");

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var note = await _context.SalesRequestNotes.FirstOrDefaultAsync(n => n.SalesRequestId == 1);
        note.Should().NotBeNull();
        note!.Note.Should().Be("Follow-up call scheduled for tomorrow");
        note.AuthorUserId.Should().Be(5);
    }

    // ── CreateInternal ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateInternal_ShouldCreateSalesRequest_WithoutCaptcha()
    {
        // Arrange
        SetUserWithRole("Sales");

        var request = new PublicSalesRequestCreate
        {
            HospitalName = "Internal Hospital",
            ContactPerson = "Jane Doe",
            Email = "jane@hospital.com",
            Phone = "01711111111",
            SubscriptionPlan = "Premium",
            Seats = 50
        };

        // Act
        var result = await _sut.CreateInternal(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        
        var savedRequest = await _context.SalesRequests.FirstOrDefaultAsync(r => r.Email == "jane@hospital.com");
        savedRequest.Should().NotBeNull();
        savedRequest!.Status.Should().Be(SalesRequestStatus.Pending);
    }

    // ── Transfer ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Transfer_ShouldChangeOwner_ToTargetUser()
    {
        // Arrange
        SetUserWithRole("Sales", userId: 1);
        
        var salesUser = new User
        {
            Id = 10,
            Email = "sales2@test.com",
            FirstName = "Sales",
            LastName = "Person",
            Role = UserRole.Sales,
            IsActive = true,
            TenantId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(salesUser);
        
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.Pending,
            OwnerSalesUserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.Transfer(1, new TransferRequest { TargetUserId = 10 });

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.OwnerSalesUserId.Should().Be(10);
    }

    public void Dispose() => _context.Dispose();
}
