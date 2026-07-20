using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Auth;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Tests.UnitTests;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPasswordService> _passwordMock;
    private readonly Mock<IJwtService> _jwtMock;
    private readonly Mock<IPermissionService> _permissionMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _sut;
    private readonly Tenant _tenant;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _passwordMock = new Mock<IPasswordService>();
        _jwtMock = new Mock<IJwtService>();
        _permissionMock = new Mock<IPermissionService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _permissionMock.Setup(p => p.GetPermissionsForRoleAsync(It.IsAny<UserRole>()))
            .ReturnsAsync(Array.Empty<string>());

        _sut = new AuthService(_context, _passwordMock.Object, _jwtMock.Object, _permissionMock.Object, _loggerMock.Object);

        _tenant = new Tenant { Id = 1, Name = "Test Hospital", Subdomain = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Tenants.Add(_tenant);
        _context.SaveChanges();

        _jwtMock.Setup(j => j.GenerateTokenPair(It.IsAny<User>()))
            .Returns(("access_token", "refresh_token"));
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
            .Returns("access_token_with_perms");
        _jwtMock.Setup(j => j.GenerateRefreshToken())
            .Returns("refresh_token");
    }

    // ── RegisterAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenRequestIsValid()
    {
        _passwordMock.Setup(p => p.HashPassword("Password1!")).Returns("hashed");

        var request = new RegisterRequest
        {
            Email = "new@test.com",
            Password = "Password1!",
            FirstName = "Jane",
            LastName = "Doe",
            TenantId = _tenant.Id,
            Role = UserRole.Doctor
        };

        var result = await _sut.RegisterAsync(request);

        result.AccessToken.Should().Be("access_token_with_perms");
        result.User.Email.Should().Be("new@test.com");
        _context.Users.Should().ContainSingle(u => u.Email == "new@test.com");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        _context.Users.Add(new User
        {
            Email = "exists@test.com", PasswordHash = "h", FirstName = "A", LastName = "B",
            Role = UserRole.Doctor, TenantId = _tenant.Id, IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "exists@test.com", Password = "Password1!", FirstName = "A",
            LastName = "B", TenantId = _tenant.Id, Role = UserRole.Doctor
        };

        await _sut.Invoking(s => s.RegisterAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenTenantIsInactive()
    {
        _tenant.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "x@test.com", Password = "Password1!", FirstName = "X",
            LastName = "Y", TenantId = _tenant.Id, Role = UserRole.Doctor
        };

        await _sut.Invoking(s => s.RegisterAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*inactive*");
    }

    // ── LoginAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsValid()
    {
        _context.Users.Add(new User
        {
            Email = "user@test.com", PasswordHash = "hashed", FirstName = "U", LastName = "S",
            Role = UserRole.Doctor, TenantId = _tenant.Id, IsActive = true, CreatedAt = DateTime.UtcNow,
            Tenant = _tenant
        });
        await _context.SaveChangesAsync();

        _passwordMock.Setup(p => p.VerifyPassword("Password1!", "hashed")).Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest { Email = "user@test.com", Password = "Password1!" });

        result.AccessToken.Should().Be("access_token_with_perms");
        result.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordWrong()
    {
        _context.Users.Add(new User
        {
            Email = "user@test.com", PasswordHash = "hashed", FirstName = "U", LastName = "S",
            Role = UserRole.Doctor, TenantId = _tenant.Id, IsActive = true, CreatedAt = DateTime.UtcNow,
            Tenant = _tenant
        });
        await _context.SaveChangesAsync();

        _passwordMock.Setup(p => p.VerifyPassword("wrong", "hashed")).Returns(false);

        await _sut.Invoking(s => s.LoginAsync(new LoginRequest { Email = "user@test.com", Password = "wrong" }))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenUserInactive()
    {
        _context.Users.Add(new User
        {
            Email = "inactive@test.com", PasswordHash = "hashed", FirstName = "U", LastName = "S",
            Role = UserRole.Doctor, TenantId = _tenant.Id, IsActive = false, CreatedAt = DateTime.UtcNow,
            Tenant = _tenant
        });
        await _context.SaveChangesAsync();

        _passwordMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), "hashed")).Returns(true);

        await _sut.Invoking(s => s.LoginAsync(new LoginRequest { Email = "inactive@test.com", Password = "x" }))
            .Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*inactive*");
    }

    // ── StaffLoginAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task StaffLoginAsync_ShouldThrow_WhenUserIsPatient()
    {
        _context.Users.Add(new User
        {
            Email = "patient@test.com", PasswordHash = "hashed", FirstName = "P", LastName = "Q",
            Role = UserRole.Patient, TenantId = _tenant.Id, IsActive = true, CreatedAt = DateTime.UtcNow,
            Tenant = _tenant
        });
        await _context.SaveChangesAsync();

        _passwordMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), "hashed")).Returns(true);

        await _sut.Invoking(s => s.StaffLoginAsync(new LoginRequest { Email = "patient@test.com", Password = "x" }))
            .Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*patient*");
    }

    [Fact]
    public async Task StaffLoginAsync_ShouldSucceed_WhenUserIsDoctor()
    {
        _context.Users.Add(new User
        {
            Email = "doctor@test.com", PasswordHash = "hashed", FirstName = "D", LastName = "R",
            Role = UserRole.Doctor, TenantId = _tenant.Id, IsActive = true, CreatedAt = DateTime.UtcNow,
            Tenant = _tenant
        });
        await _context.SaveChangesAsync();

        _passwordMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), "hashed")).Returns(true);

        var result = await _sut.StaffLoginAsync(new LoginRequest { Email = "doctor@test.com", Password = "x" });

        result.AccessToken.Should().Be("access_token_with_perms");
    }

    // ── RefreshTokenAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenTokenValid()
    {
        _context.Users.Add(new User
        {
            Email = "u@test.com", PasswordHash = "h", FirstName = "A", LastName = "B",
            Role = UserRole.Doctor, TenantId = _tenant.Id, IsActive = true, CreatedAt = DateTime.UtcNow,
            Tenant = _tenant, RefreshToken = "valid_token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        });
        await _context.SaveChangesAsync();

        var result = await _sut.RefreshTokenAsync("valid_token");

        result.AccessToken.Should().Be("access_token_with_perms");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrow_WhenTokenExpired()
    {
        _context.Users.Add(new User
        {
            Email = "u@test.com", PasswordHash = "h", FirstName = "A", LastName = "B",
            Role = UserRole.Doctor, TenantId = _tenant.Id, IsActive = true, CreatedAt = DateTime.UtcNow,
            Tenant = _tenant, RefreshToken = "expired_token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1)
        });
        await _context.SaveChangesAsync();

        await _sut.Invoking(s => s.RefreshTokenAsync("expired_token"))
            .Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*expired*");
    }

    // ── LogoutAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_ShouldClearRefreshToken()
    {
        var user = new User
        {
            Email = "u@test.com", PasswordHash = "h", FirstName = "A", LastName = "B",
            Role = UserRole.Doctor, TenantId = _tenant.Id, IsActive = true, CreatedAt = DateTime.UtcNow,
            RefreshToken = "token", RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _sut.LogoutAsync(user.Id);

        var updated = await _context.Users.FindAsync(user.Id);
        updated!.RefreshToken.Should().BeNull();
        updated.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public async Task LogoutAsync_ShouldThrow_WhenUserNotFound()
    {
        await _sut.Invoking(s => s.LogoutAsync(9999))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
