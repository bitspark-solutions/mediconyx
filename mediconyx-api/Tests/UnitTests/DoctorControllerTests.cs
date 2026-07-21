using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using mediconyx_api.Controllers;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Tests.UnitTests;

public class DoctorControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DoctorController _sut;
    private readonly Tenant _tenant;

    public DoctorControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var passwordMock = new Mock<IPasswordService>();
        var loggerMock = new Mock<ILogger<DoctorController>>();
        _sut = new DoctorController(_context, passwordMock.Object, loggerMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim("TenantId", "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _tenant = new Tenant { Id = 1, Name = "Test Hospital", Subdomain = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Tenants.Add(_tenant);
        _context.SaveChanges();
    }

    private User AddUser(int id, string email, UserRole role = UserRole.Admin, int? tenantId = null)
    {
        var user = new User
        {
            Id = id, Email = email, PasswordHash = "hash",
            FirstName = "First", LastName = "Last",
            Role = role, TenantId = tenantId ?? _tenant.Id, IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private static AttachDoctorProfileRequest ValidRequest(int userId) => new()
    {
        UserId = userId,
        MedicalLicenseNumber = "BMA-12345",
        Specialization = MedicalSpecializations.Cardiology,
        ConsultationDurationMinutes = 20,
        ConsultationFee = 1500
    };

    // ── AttachProfile ────────────────────────────────────────────────────────

    [Fact]
    public async Task AttachProfile_ShouldCreateProfileAndGrantDoctorRole_WhenStaffUser()
    {
        var staff = AddUser(10, "director@test.com", UserRole.Admin);

        var result = await _sut.AttachProfile(ValidRequest(staff.Id));

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var profile = await _context.DoctorProfiles.SingleAsync(d => d.UserId == staff.Id);
        profile.TenantId.Should().Be(_tenant.Id);
        profile.Specialization.Should().Be(MedicalSpecializations.Cardiology);
        profile.IsAcceptingPatients.Should().BeTrue();

        var assignment = await _context.UserRoleAssignments
            .SingleAsync(a => a.UserId == staff.Id && a.Role == UserRole.Doctor);
        assignment.GrantedByUserId.Should().Be(1);

        // Primary role untouched — the user is still an Admin who also practices
        (await _context.Users.FindAsync(staff.Id))!.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task AttachProfile_ShouldNotAddAssignment_WhenPrimaryRoleAlreadyDoctor()
    {
        var doctor = AddUser(11, "doc@test.com", UserRole.Doctor);

        var result = await _sut.AttachProfile(ValidRequest(doctor.Id));

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        _context.UserRoleAssignments.Should().BeEmpty();
    }

    [Fact]
    public async Task AttachProfile_ShouldReturnBadRequest_WhenUserAlreadyHasProfile()
    {
        var doctor = AddUser(12, "doc2@test.com", UserRole.Doctor);
        _context.DoctorProfiles.Add(new DoctorProfile
        {
            UserId = doctor.Id, TenantId = _tenant.Id,
            Specialization = MedicalSpecializations.General,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.AttachProfile(ValidRequest(doctor.Id));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _context.DoctorProfiles.Count(d => d.UserId == doctor.Id).Should().Be(1);
    }

    [Fact]
    public async Task AttachProfile_ShouldReturnNotFound_WhenUserInOtherTenant()
    {
        var outsider = AddUser(13, "outsider@other.com", UserRole.Admin, tenantId: 99);

        var result = await _sut.AttachProfile(ValidRequest(outsider.Id));

        result.Result.Should().BeOfType<NotFoundObjectResult>();
        _context.DoctorProfiles.Should().BeEmpty();
    }

    [Fact]
    public async Task AttachProfile_ShouldReturnBadRequest_WhenSpecializationInvalid()
    {
        var staff = AddUser(14, "nurse@test.com", UserRole.Nurse);
        var request = ValidRequest(staff.Id);
        request.Specialization = "Not A Specialty";

        var result = await _sut.AttachProfile(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _context.DoctorProfiles.Should().BeEmpty();
    }

    [Fact]
    public async Task AttachProfile_ShouldReturnBadRequest_WhenDepartmentInOtherTenant()
    {
        var staff = AddUser(15, "rec@test.com", UserRole.Receptionist);
        _context.Departments.Add(new Department
        {
            Id = 5, TenantId = 99, Name = "Other Dept", Code = "OD", IsActive = true
        });
        await _context.SaveChangesAsync();

        var request = ValidRequest(staff.Id);
        request.DepartmentId = 5;

        var result = await _sut.AttachProfile(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _context.DoctorProfiles.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
