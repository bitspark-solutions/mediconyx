using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Infrastructure;
using mediconyx_api.Services.Users;

namespace mediconyx_api.Tests.UnitTests;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPasswordService> _passwordMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _sut;
    private readonly Tenant _tenant;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _passwordMock = new Mock<IPasswordService>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _sut = new UserService(_context, _passwordMock.Object, _loggerMock.Object);

        _tenant = new Tenant { Id = 1, Name = "Test Hospital", Subdomain = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Tenants.Add(_tenant);
        _context.SaveChanges();
    }

    private User AddUser(int id, string email, UserRole role = UserRole.Doctor, bool isActive = true)
    {
        var user = new User
        {
            Id = id, Email = email, PasswordHash = "hash",
            FirstName = "First", LastName = "Last",
            Role = role, TenantId = _tenant.Id, IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    // ── GetCurrentUserAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnDto_WhenUserExists()
    {
        AddUser(1, "u@test.com");

        var result = await _sut.GetCurrentUserAsync(1);

        result.Email.Should().Be("u@test.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldThrow_WhenUserNotFound()
    {
        await _sut.Invoking(s => s.GetCurrentUserAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetUsersAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_ShouldFilterByTenant_WhenNoTenantIdInParams()
    {
        AddUser(1, "a@test.com");
        _context.Users.Add(new User
        {
            Id = 2, Email = "b@other.com", PasswordHash = "h", FirstName = "B", LastName = "C",
            Role = UserRole.Doctor, TenantId = 99, IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(callerTenantId: _tenant.Id, new UserQueryParams { PageSize = 20, PageNumber = 1 });

        result.Users.Should().ContainSingle(u => u.Email == "a@test.com");
    }

    [Fact]
    public async Task GetUsersAsync_ShouldFilterByRole()
    {
        AddUser(1, "doc@test.com", UserRole.Doctor);
        AddUser(2, "rec@test.com", UserRole.Receptionist);

        var result = await _sut.GetUsersAsync(null, new UserQueryParams
        {
            Role = UserRole.Doctor, PageSize = 20, PageNumber = 1
        });

        result.Users.Should().ContainSingle().Which.Email.Should().Be("doc@test.com");
    }

    [Fact]
    public async Task GetUsersAsync_ShouldSearchByEmail()
    {
        AddUser(1, "findme@test.com");
        AddUser(2, "other@test.com");

        var result = await _sut.GetUsersAsync(null, new UserQueryParams
        {
            SearchTerm = "findme", PageSize = 20, PageNumber = 1
        });

        result.Users.Should().ContainSingle().Which.Email.Should().Be("findme@test.com");
    }

    // ── GetUserByIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturn_WhenAdminRequestsOther()
    {
        AddUser(1, "admin@test.com", UserRole.Admin);
        AddUser(2, "target@test.com");

        var result = await _sut.GetUserByIdAsync(requestedUserId: 2, callerUserId: 1, UserRole.Admin);

        result.Email.Should().Be("target@test.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldThrow_WhenNonAdminRequestsOther()
    {
        AddUser(1, "doc@test.com", UserRole.Doctor);
        AddUser(2, "other@test.com", UserRole.Doctor);

        await _sut.Invoking(s => s.GetUserByIdAsync(requestedUserId: 2, callerUserId: 1, UserRole.Doctor))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldThrow_WhenUserNotFound()
    {
        await _sut.Invoking(s => s.GetUserByIdAsync(999, 1, UserRole.Admin))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── CreateUserAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_ShouldPersistUser_WhenValid()
    {
        _passwordMock.Setup(p => p.HashPassword("Pass123!")).Returns("hashed");

        var request = new CreateUserRequest
        {
            Email = "new@test.com", Password = "Pass123!",
            FirstName = "New", LastName = "User",
            Role = UserRole.Doctor, TenantId = _tenant.Id
        };

        var result = await _sut.CreateUserAsync(request, adminUserId: 1);

        result.Email.Should().Be("new@test.com");
        _context.Users.Should().ContainSingle(u => u.Email == "new@test.com");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenEmailInvalid()
    {
        var request = new CreateUserRequest
        {
            Email = "not-an-email", Password = "Pass123!",
            FirstName = "X", LastName = "Y",
            Role = UserRole.Doctor, TenantId = _tenant.Id
        };

        await _sut.Invoking(s => s.CreateUserAsync(request, 1))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*email*");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenPasswordTooShort()
    {
        var request = new CreateUserRequest
        {
            Email = "x@test.com", Password = "12345",
            FirstName = "X", LastName = "Y",
            Role = UserRole.Doctor, TenantId = _tenant.Id
        };

        await _sut.Invoking(s => s.CreateUserAsync(request, 1))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*6 characters*");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        AddUser(1, "dup@test.com");

        var request = new CreateUserRequest
        {
            Email = "dup@test.com", Password = "Pass123!",
            FirstName = "D", LastName = "U",
            Role = UserRole.Doctor, TenantId = _tenant.Id
        };

        await _sut.Invoking(s => s.CreateUserAsync(request, 1))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
    }

    // ── UpdateUserAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateFields_WhenCallerIsSelf()
    {
        AddUser(1, "u@test.com", UserRole.Doctor);

        var request = new UpdateUserRequest { FirstName = "Updated" };
        var result = await _sut.UpdateUserAsync(1, request, callerUserId: 1, UserRole.Doctor);

        result.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrow_WhenNonAdminChangesRole()
    {
        AddUser(1, "u@test.com", UserRole.Doctor);

        var request = new UpdateUserRequest { Role = UserRole.Admin };

        await _sut.Invoking(s => s.UpdateUserAsync(1, request, callerUserId: 1, UserRole.Doctor))
            .Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*role*");
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldAllowAdminToChangeRole()
    {
        AddUser(1, "u@test.com", UserRole.Doctor);

        var request = new UpdateUserRequest { Role = UserRole.Receptionist };
        var result = await _sut.UpdateUserAsync(1, request, callerUserId: 99, UserRole.Admin);

        result.Role.Should().Be("Receptionist");
    }

    // ── DeactivateUserAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateUserAsync_ShouldSetInactive()
    {
        AddUser(1, "u@test.com");

        await _sut.DeactivateUserAsync(userId: 1, callerUserId: 2);

        var user = await _context.Users.FindAsync(1);
        user!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUserAsync_ShouldThrow_WhenDeletingSelf()
    {
        await _sut.Invoking(s => s.DeactivateUserAsync(userId: 1, callerUserId: 1))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*own account*");
    }

    // ── ToggleActiveAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActiveAsync_ShouldFlipIsActive()
    {
        AddUser(1, "u@test.com", isActive: true);

        var result = await _sut.ToggleActiveAsync(userId: 1, callerUserId: 2);

        result.IsActive.Should().BeFalse();
    }

    // ── ChangePasswordAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_ShouldUpdateHash_WhenCurrentPasswordCorrect()
    {
        AddUser(1, "u@test.com");
        _passwordMock.Setup(p => p.VerifyPassword("OldPass!", "hash")).Returns(true);
        _passwordMock.Setup(p => p.HashPassword("NewPass123!")).Returns("new_hash");

        await _sut.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass!", NewPassword = "NewPass123!"
        });

        var user = await _context.Users.FindAsync(1);
        user!.PasswordHash.Should().Be("new_hash");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrow_WhenCurrentPasswordWrong()
    {
        AddUser(1, "u@test.com");
        _passwordMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), "hash")).Returns(false);

        await _sut.Invoking(s => s.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "wrong", NewPassword = "NewPass123!"
        })).Should().ThrowAsync<InvalidOperationException>().WithMessage("*incorrect*");
    }

    // ── ResetPasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_ShouldUpdateHash()
    {
        AddUser(1, "u@test.com");
        _passwordMock.Setup(p => p.HashPassword("Reset123!")).Returns("reset_hash");

        await _sut.ResetPasswordAsync(new ResetPasswordRequest { UserId = 1, NewPassword = "Reset123!" }, adminUserId: 99);

        var user = await _context.Users.FindAsync(1);
        user!.PasswordHash.Should().Be("reset_hash");
    }

    // ── Designation / department / additional roles ──────────────────────────

    private Department AddDepartment(int id, string name, int? tenantId = null)
    {
        var dept = new Department
        {
            Id = id, TenantId = tenantId ?? _tenant.Id,
            Name = name, Code = name.ToUpper(), IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Departments.Add(dept);
        _context.SaveChanges();
        return dept;
    }

    [Fact]
    public async Task CreateUserAsync_ShouldPersistDesignationAndDepartment()
    {
        AddDepartment(1, "Cardiology");
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");

        var request = new CreateUserRequest
        {
            Email = "dir@test.com", Password = "Pass123!",
            FirstName = "Dir", LastName = "Ector",
            Role = UserRole.Admin, TenantId = _tenant.Id,
            Designation = "Director of Cardiology", PrimaryDepartmentId = 1
        };

        var result = await _sut.CreateUserAsync(request, adminUserId: 1);

        result.Designation.Should().Be("Director of Cardiology");
        result.PrimaryDepartmentId.Should().Be(1);
        result.PrimaryDepartmentName.Should().Be("Cardiology");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenDepartmentInOtherTenant()
    {
        AddDepartment(1, "Other Dept", tenantId: 99);
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");

        var request = new CreateUserRequest
        {
            Email = "x@test.com", Password = "Pass123!",
            FirstName = "X", LastName = "Y",
            Role = UserRole.Nurse, TenantId = _tenant.Id,
            PrimaryDepartmentId = 1
        };

        await _sut.Invoking(s => s.CreateUserAsync(request, 1))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*department*");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldGrantAdditionalRoles_ExcludingPrimary()
    {
        _passwordMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");

        var request = new CreateUserRequest
        {
            Email = "multi@test.com", Password = "Pass123!",
            FirstName = "M", LastName = "R",
            Role = UserRole.Admin, TenantId = _tenant.Id,
            AdditionalRoles = new[] { UserRole.Admin, UserRole.Doctor, UserRole.Accountant }
        };

        var result = await _sut.CreateUserAsync(request, adminUserId: 42);

        result.AdditionalRoles.Should().BeEquivalentTo("Doctor", "Accountant");
        var rows = _context.UserRoleAssignments.Where(a => a.UserId == result.Id).ToList();
        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(a => a.GrantedByUserId == 42);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldSyncAdditionalRoles_GrantAndRevoke()
    {
        var user = AddUser(1, "u@test.com", UserRole.Receptionist);
        _context.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id, Role = UserRole.Accountant, GrantedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Designation = "Front Desk Lead",
            AdditionalRoles = new[] { UserRole.Doctor }
        };
        var result = await _sut.UpdateUserAsync(1, request, callerUserId: 99, UserRole.Admin);

        result.Designation.Should().Be("Front Desk Lead");
        result.AdditionalRoles.Should().BeEquivalentTo("Doctor");
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrow_WhenNonAdminChangesAdditionalRoles()
    {
        AddUser(1, "u@test.com", UserRole.Doctor);

        var request = new UpdateUserRequest { AdditionalRoles = new[] { UserRole.Admin } };

        await _sut.Invoking(s => s.UpdateUserAsync(1, request, callerUserId: 1, UserRole.Doctor))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldClearDesignation_WhenBlank()
    {
        var user = AddUser(1, "u@test.com", UserRole.Nurse);
        user.Designation = "Head Nurse";
        await _context.SaveChangesAsync();

        var result = await _sut.UpdateUserAsync(1, new UpdateUserRequest { Designation = " " },
            callerUserId: 1, UserRole.Nurse);

        result.Designation.Should().BeNull();
    }

    public void Dispose() => _context.Dispose();
}
