using FluentAssertions;
using mediconyx_api.Services.Infrastructure;
using Xunit;
using System;

namespace mediconyx_api.Tests.UnitTests;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyString()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_ShouldReturnDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt generates different salts
    }

    [Fact]
    public void HashPassword_ShouldThrowException_WhenPasswordIsNull()
    {
        // Arrange
        string? password = null;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(password!));
    }

    [Fact]
    public void HashPassword_ShouldThrowException_WhenPasswordIsEmpty()
    {
        // Arrange
        var password = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(password));
    }

    [Fact]
    public void HashPassword_ShouldThrowException_WhenPasswordIsWhitespace()
    {
        // Arrange
        var password = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(password));
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordIsNull()
    {
        // Arrange
        var hash = _passwordService.HashPassword("TestPassword123!");

        // Act
        var result = _passwordService.VerifyPassword(null!, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenHashIsNull()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var result = _passwordService.VerifyPassword(password, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenHashIsEmpty()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var result = _passwordService.VerifyPassword(password, string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("12345")]
    [InlineData("a")]
    [InlineData("VeryLongPasswordThatExceedsNormalLengthButShouldStillWork123456789")]
    public void HashPassword_ShouldHandleVariousPasswordLengths(string password)
    {
        // Act
        var hash = _passwordService.HashPassword(password);
        var verifyResult = _passwordService.VerifyPassword(password, hash);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        verifyResult.Should().BeTrue();
    }
}

