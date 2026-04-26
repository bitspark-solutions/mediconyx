using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using mediconyx_api.Controllers;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Users;
using Xunit;

namespace mediconyx_api.Tests.UnitTests;

public class UserManagementControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UserManagementController>> _loggerMock;
    private readonly UserManagementController _controller;

    public UserManagementControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UserManagementController>>();
        _controller = new UserManagementController(_userServiceMock.Object, _loggerMock.Object);
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
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUser_WhenAuthenticated()
    {
        // Arrange
        SetupAuthenticatedUser(1, UserRole.Admin, 1);
        var dto = new UserDetailDto { Id = 1, Email = "test@example.com" };
        _userServiceMock.Setup(x => x.GetCurrentUserAsync(1)).ReturnsAsync(dto);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result.Result!;
        ((UserDetailDto)ok.Value!).Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
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
        SetupAuthenticatedUser(1, UserRole.Admin, 1);
        var request = new CreateUserRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = 1
        };
        var dto = new UserDetailDto { Id = 5, Email = request.Email };
        _userServiceMock.Setup(x => x.CreateUserAsync(request, 1)).ReturnsAsync(dto);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenServiceThrowsInvalidOperation()
    {
        // Arrange
        SetupAuthenticatedUser(1, UserRole.Admin, 1);
        var request = new CreateUserRequest
        {
            Email = "dup@example.com",
            Password = "Password123!",
            FirstName = "Dup",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = 1
        };
        _userServiceMock.Setup(x => x.CreateUserAsync(request, 1))
            .ThrowsAsync(new InvalidOperationException("User already exists."));

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WhenValidUpdate()
    {
        // Arrange
        SetupAuthenticatedUser(1, UserRole.Doctor, 1);
        var request = new UpdateUserRequest { FirstName = "Updated", LastName = "Name" };
        var dto = new UserDetailDto { Id = 1, FirstName = "Updated", LastName = "Name" };
        _userServiceMock.Setup(x => x.UpdateUserAsync(1, request, 1, UserRole.Doctor)).ReturnsAsync(dto);

        // Act
        var result = await _controller.UpdateUser(1, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result.Result!;
        ((UserDetailDto)ok.Value!).FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        SetupAuthenticatedUser(1, UserRole.Doctor, 1);
        var request = new ChangePasswordRequest { CurrentPassword = "Old!", NewPassword = "New123!" };
        _userServiceMock.Setup(x => x.ChangePasswordAsync(1, request)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenServiceThrowsArgumentException()
    {
        // Arrange
        SetupAuthenticatedUser(1, UserRole.Doctor, 1);
        var request = new ChangePasswordRequest { CurrentPassword = "Wrong", NewPassword = "New123!" };
        _userServiceMock.Setup(x => x.ChangePasswordAsync(1, request))
            .ThrowsAsync(new ArgumentException("Current password is incorrect."));

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}

