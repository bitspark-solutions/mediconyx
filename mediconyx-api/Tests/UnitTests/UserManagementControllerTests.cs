using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using mediconyx_api.Controllers;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services;
using Xunit;

namespace mediconyx_api.Tests.UnitTests;

public class UserManagementControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ILogger<UserManagementController>> _loggerMock;
    private readonly UserManagementController _controller;
    private readonly Tenant _testTenant;

    public UserManagementControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Setup mocks
        _passwordServiceMock = new Mock<IPasswordService>();
        _loggerMock = new Mock<ILogger<UserManagementController>>();

        // Setup controller
        _controller = new UserManagementController(
            _context,
            _passwordServiceMock.Object,
            _loggerMock.Object);

        // Setup test tenant
        _testTenant = new Tenant
        {
            Id = 1,
            Name = "Test Hospital",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(_testTenant);
        _context.SaveChanges();
    }

    private void SetupAuthenticatedUser(int userId, UserRole role, int tenantId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("TenantId", tenantId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUser_WhenAuthenticated()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Admin,
            TenantId = _testTenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        SetupAuthenticatedUser(1, UserRole.Admin, _testTenant.Id);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var dto = okResult!.Value as UserDetailDto;
        dto.Should().NotBeNull();
        dto!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        SetupAuthenticatedUser(1, UserRole.Admin, _testTenant.Id);
        _passwordServiceMock.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed_password");

        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = _testTenant.Id
        };

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        _passwordServiceMock.Verify(x => x.HashPassword("Password123!"), Times.Once);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenEmailInvalid()
    {
        // Arrange
        SetupAuthenticatedUser(1, UserRole.Admin, _testTenant.Id);

        var request = new CreateUserRequest
        {
            Email = "invalid-email",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = _testTenant.Id
        };

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenPasswordTooShort()
    {
        // Arrange
        SetupAuthenticatedUser(1, UserRole.Admin, _testTenant.Id);

        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "12345", // Too short
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = _testTenant.Id
        };

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WhenValidUpdate()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = _testTenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        SetupAuthenticatedUser(1, UserRole.Doctor, _testTenant.Id);

        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };

        // Act
        var result = await _controller.UpdateUser(1, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.FirstName.Should().Be("Updated");
        updatedUser.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnOk_WhenPasswordValid()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = "old_hash",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = _testTenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        SetupAuthenticatedUser(1, UserRole.Doctor, _testTenant.Id);

        _passwordServiceMock.Setup(x => x.VerifyPassword("OldPassword123!", "old_hash"))
            .Returns(true);
        _passwordServiceMock.Setup(x => x.HashPassword("NewPassword123!"))
            .Returns("new_hash");

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _passwordServiceMock.Verify(x => x.VerifyPassword("OldPassword123!", "old_hash"), Times.Once);
        _passwordServiceMock.Verify(x => x.HashPassword("NewPassword123!"), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenCurrentPasswordIncorrect()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = "old_hash",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = _testTenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        SetupAuthenticatedUser(1, UserRole.Doctor, _testTenant.Id);

        _passwordServiceMock.Setup(x => x.VerifyPassword("WrongPassword", "old_hash"))
            .Returns(false);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

