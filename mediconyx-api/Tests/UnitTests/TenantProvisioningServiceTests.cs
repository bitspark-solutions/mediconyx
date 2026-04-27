using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Tests.UnitTests;

public class TenantProvisioningServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPasswordService> _passwordMock;
    private readonly Mock<ILogger<TenantProvisioningService>> _loggerMock;
    private readonly TenantProvisioningService _sut;

    public TenantProvisioningServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _passwordMock = new Mock<IPasswordService>();
        _loggerMock = new Mock<ILogger<TenantProvisioningService>>();
        _sut = new TenantProvisioningService(_context, _passwordMock.Object, _loggerMock.Object);
    }

    private SalesRequest CreateCompleteSalesRequest(int id, SalesRequestStatus status = SalesRequestStatus.AwaitingPayment, PaymentStatus paymentStatus = PaymentStatus.Paid)
    {
        var request = new SalesRequest
        {
            Id = id,
            HospitalName = "Test Hospital",
            ContactPerson = "John Doe",
            Email = "test@hospital.com",
            Phone = "01700000000",
            Status = status,
            PaymentStatus = paymentStatus,
            SubscriptionPlan = "Standard",
            Seats = 10,
            CreatedAt = DateTime.UtcNow
        };

        var profile = new HospitalProfile
        {
            SalesRequestId = id,
            HospitalLegalName = "Test Hospital LLC",
            LicenseNumber = "LIC-12345",
            TaxId = "TAX-67890",
            HospitalType = HospitalType.Private,
            BedCapacity = 100,
            AddressLine1 = "123 Main St",
            City = "Dhaka",
            StateProvince = "Dhaka",
            PostalCode = "1200",
            Country = "Bangladesh",
            PrimaryContactName = "John Doe",
            PrimaryContactEmail = "admin@testhospital.com",
            PrimaryContactPhone = "01700000000"
        };

        var modules = new ModuleSelection
        {
            SalesRequestId = id,
            OpdManagement = true,
            IpdManagement = true,
            AppointmentScheduling = true,
            BillingInvoicing = true,
            PharmacyManagement = true,
            Laboratory = true,
            RadiologyImaging = false,
            InventoryManagement = true,
            HrPayroll = false,
            EmergencyServices = true,
            Telemedicine = false,
            AnalyticsReporting = true
        };

        _context.SalesRequests.Add(request);
        _context.HospitalProfiles.Add(profile);
        _context.ModuleSelections.Add(modules);
        _context.SaveChanges();

        // Attach navigation properties
        request.HospitalProfile = profile;
        request.ModuleSelection = modules;

        return request;
    }

    // ── ProvisionTenantAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ProvisionTenantAsync_ShouldCreateTenantAndUser_WhenAllDataValid()
    {
        // Arrange
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        var request = CreateCompleteSalesRequest(1);

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.TenantId.Should().BeGreaterThan(0);
        result.AdminUserId.Should().BeGreaterThan(0);
        result.Subdomain.Should().NotBeNullOrEmpty();
        result.AdminEmail.Should().Be("admin@testhospital.com");
        result.TemporaryPassword.Should().NotBeNullOrEmpty().And.HaveLength(16);

        // Verify tenant was created
        var tenant = await _context.Tenants.FindAsync(result.TenantId);
        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be("Test Hospital LLC");
        tenant.Subdomain.Should().Be(result.Subdomain);
        tenant.SubscriptionPlan.Should().Be("Standard");
        tenant.UserLicenses.Should().Be(10);
        tenant.IsActive.Should().BeTrue();

        // Verify admin user was created
        var admin = await _context.Users.FindAsync(result.AdminUserId);
        admin.Should().NotBeNull();
        admin!.Email.Should().Be("admin@testhospital.com");
        admin.Role.Should().Be(UserRole.Admin);
        admin.TenantId.Should().Be(result.TenantId);
        admin.IsActive.Should().BeTrue();

        // Verify sales request updated
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.TenantId.Should().Be(result.TenantId);
        updatedRequest.Status.Should().Be(SalesRequestStatus.Active);
        updatedRequest.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldConfigureModules_WhenModuleSelectionExists()
    {
        // Arrange
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        var request = CreateCompleteSalesRequest(1);

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        var tenant = await _context.Tenants.FindAsync(result.TenantId);
        tenant!.ModuleOpdEnabled.Should().BeTrue();
        tenant.ModuleIpdEnabled.Should().BeTrue();
        tenant.ModuleAppointmentEnabled.Should().BeTrue();
        tenant.ModuleBillingEnabled.Should().BeTrue();
        tenant.ModulePharmacyEnabled.Should().BeTrue();
        tenant.ModuleLaboratoryEnabled.Should().BeTrue();
        tenant.ModuleRadiologyEnabled.Should().BeFalse();
        tenant.ModuleInventoryEnabled.Should().BeTrue();
        tenant.ModuleHrPayrollEnabled.Should().BeFalse();
        tenant.ModuleEmergencyEnabled.Should().BeTrue();
        tenant.ModuleTelemedicineEnabled.Should().BeFalse();
        tenant.ModuleAnalyticsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldGenerateUniqueSubdomain()
    {
        // Arrange
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        
        // First tenant
        var request1 = CreateCompleteSalesRequest(1);
        var result1 = await _sut.ProvisionTenantAsync(1);

        // Second tenant with same hospital name
        var request2 = new SalesRequest
        {
            Id = 2,
            HospitalName = "Test Hospital LLC",
            Status = SalesRequestStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };
        var profile2 = new HospitalProfile
        {
            SalesRequestId = 2,
            HospitalLegalName = "Test Hospital LLC",
            LicenseNumber = "LIC-99999",
            TaxId = "TAX-99999",
            HospitalType = HospitalType.Private,
            BedCapacity = 50,
            AddressLine1 = "456 Other St",
            City = "Chittagong",
            StateProvince = "Chittagong",
            PostalCode = "4000",
            Country = "Bangladesh",
            PrimaryContactName = "Jane Doe",
            PrimaryContactEmail = "admin2@testhospital.com",
            PrimaryContactPhone = "01711111111"
        };
        var modules2 = new ModuleSelection
        {
            SalesRequestId = 2,
            OpdManagement = true,
            IpdManagement = false,
            AppointmentScheduling = true
        };
        _context.SalesRequests.Add(request2);
        _context.HospitalProfiles.Add(profile2);
        _context.ModuleSelections.Add(modules2);
        await _context.SaveChangesAsync();

        // Act
        var result2 = await _sut.ProvisionTenantAsync(2);

        // Assert
        result2.Success.Should().BeTrue();
        result2.Subdomain.Should().NotBe(result1.Subdomain);
        result2.Subdomain.Should().StartWith("test-hospital-llc");
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldFail_WhenSalesRequestNotFound()
    {
        // Act
        var result = await _sut.ProvisionTenantAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldFail_WhenHospitalProfileMissing()
    {
        // Arrange
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "No Profile Hospital",
            Status = SalesRequestStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalesRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Hospital profile is required");
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldFail_WhenModuleSelectionMissing()
    {
        // Arrange
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "No Modules Hospital",
            Status = SalesRequestStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };
        var profile = new HospitalProfile
        {
            SalesRequestId = 1,
            HospitalLegalName = "No Modules Hospital LLC",
            LicenseNumber = "LIC-12345",
            TaxId = "TAX-67890",
            HospitalType = HospitalType.Private,
            BedCapacity = 50,
            AddressLine1 = "123 Main St",
            City = "Dhaka",
            StateProvince = "Dhaka",
            PostalCode = "1200",
            Country = "Bangladesh",
            PrimaryContactName = "John Doe",
            PrimaryContactEmail = "admin@test.com",
            PrimaryContactPhone = "01700000000"
        };
        _context.SalesRequests.Add(request);
        _context.HospitalProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Module selection is required");
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldSanitizeHospitalNameForSubdomain()
    {
        // Arrange
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Saint Mary's Hospital & Clinic (Pvt) Ltd.",
            Status = SalesRequestStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };
        var profile = new HospitalProfile
        {
            SalesRequestId = 1,
            HospitalLegalName = "Saint Mary's Hospital & Clinic (Pvt) Ltd.",
            LicenseNumber = "LIC-12345",
            TaxId = "TAX-67890",
            HospitalType = HospitalType.Private,
            BedCapacity = 200,
            AddressLine1 = "123 Main St",
            City = "Dhaka",
            StateProvince = "Dhaka",
            PostalCode = "1200",
            Country = "Bangladesh",
            PrimaryContactName = "John Doe",
            PrimaryContactEmail = "admin@stmarys.com",
            PrimaryContactPhone = "01700000000"
        };
        var modules = new ModuleSelection { SalesRequestId = 1, OpdManagement = true };
        _context.SalesRequests.Add(request);
        _context.HospitalProfiles.Add(profile);
        _context.ModuleSelections.Add(modules);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Subdomain.Should().NotContain("&").And.NotContain("(").And.NotContain(")").And.NotContain(".");
        result.Subdomain.Should().MatchRegex("^[a-z0-9-]+$");
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldSetSubscriptionDates_WhenContractStartDateProvided()
    {
        // Arrange
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        var contractStart = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Paid,
            ContractStartDate = contractStart,
            ContractDuration = 24, // 24 months
            CreatedAt = DateTime.UtcNow
        };
        var profile = new HospitalProfile
        {
            SalesRequestId = 1,
            HospitalLegalName = "Test Hospital LLC",
            LicenseNumber = "LIC-12345",
            TaxId = "TAX-67890",
            HospitalType = HospitalType.Private,
            BedCapacity = 100,
            AddressLine1 = "123 Main St",
            City = "Dhaka",
            StateProvince = "Dhaka",
            PostalCode = "1200",
            Country = "Bangladesh",
            PrimaryContactName = "John Doe",
            PrimaryContactEmail = "admin@test.com",
            PrimaryContactPhone = "01700000000"
        };
        var modules = new ModuleSelection { SalesRequestId = 1, OpdManagement = true };
        _context.SalesRequests.Add(request);
        _context.HospitalProfiles.Add(profile);
        _context.ModuleSelections.Add(modules);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        var tenant = await _context.Tenants.FindAsync(result.TenantId);
        tenant!.SubscriptionStartDate.Should().Be(contractStart);
        tenant.SubscriptionExpiresAt.Should().Be(contractStart.AddMonths(24));
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldExtractFirstAndLastName_FromFullName()
    {
        // Arrange
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        var request = new SalesRequest
        {
            Id = 1,
            HospitalName = "Test Hospital",
            Status = SalesRequestStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };
        var profile = new HospitalProfile
        {
            SalesRequestId = 1,
            HospitalLegalName = "Test Hospital LLC",
            LicenseNumber = "LIC-12345",
            TaxId = "TAX-67890",
            HospitalType = HospitalType.Private,
            BedCapacity = 100,
            AddressLine1 = "123 Main St",
            City = "Dhaka",
            StateProvince = "Dhaka",
            PostalCode = "1200",
            Country = "Bangladesh",
            PrimaryContactName = "Dr. Muhammad Abdul Rahman Khan", // Multi-word name
            PrimaryContactEmail = "admin@test.com",
            PrimaryContactPhone = "01700000000"
        };
        var modules = new ModuleSelection { SalesRequestId = 1, OpdManagement = true };
        _context.SalesRequests.Add(request);
        _context.HospitalProfiles.Add(profile);
        _context.ModuleSelections.Add(modules);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        var admin = await _context.Users.FindAsync(result.AdminUserId);
        admin!.FirstName.Should().Be("Dr."); // First word
        admin.LastName.Should().Be("Muhammad Abdul Rahman Khan"); // Rest of words
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldCreateTimelineEvent_WhenSuccessful()
    {
        // Arrange
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        var request = CreateCompleteSalesRequest(1);

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        var timelineEvents = _context.OnboardingTimelineEvents.Where(e => e.SalesRequestId == 1).ToList();
        timelineEvents.Should().ContainSingle(e => e.Type == "Provisioned");
        var provisionEvent = timelineEvents.First(e => e.Type == "Provisioned");
        provisionEvent.Description.Should().Contain("Tenant provisioned successfully");
        provisionEvent.Description.Should().Contain(result.Subdomain);
    }

    [Fact]
    public async Task ProvisionTenantAsync_ShouldRollbackTransaction_WhenErrorOccurs()
    {
        // Arrange - create a scenario where second SaveChanges might fail
        // We'll simulate by not setting up password mock, which would cause exception
        var request = CreateCompleteSalesRequest(1);
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Throws<InvalidOperationException>();

        // Act
        var result = await _sut.ProvisionTenantAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        
        // Verify no tenant was created (rolled back)
        var tenantCount = await _context.Tenants.CountAsync();
        tenantCount.Should().Be(0);
        
        // Verify no user was created (rolled back)
        var userCount = await _context.Users.CountAsync();
        userCount.Should().Be(0);
        
        // Sales request should not have tenant assigned
        var updatedRequest = await _context.SalesRequests.FindAsync(1);
        updatedRequest!.TenantId.Should().BeNull();
    }

    public void Dispose() => _context.Dispose();
}
