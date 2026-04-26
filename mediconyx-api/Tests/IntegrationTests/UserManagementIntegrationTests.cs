using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using Xunit;

namespace mediconyx_api.Tests.IntegrationTests;

// Note: Integration tests require Program class to be accessible
// For top-level statements, we'll use a different approach
public class UserManagementIntegrationTests : IDisposable
{
    // Integration tests will be implemented when Program class is accessible
    // For now, these are placeholder tests that demonstrate the structure
    private ApplicationDbContext? _context;
    private Tenant? _testTenant;
    private User? _adminUser;

    public UserManagementIntegrationTests()
    {
        // Setup in-memory database for integration testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "IntegrationTestDb_" + Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        InitializeTestData();
    }

    private void InitializeTestData()
    {
        if (_context == null) return;

        // Create test tenant
        _testTenant = new Tenant
        {
            Name = "Test Hospital",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(_testTenant);
        _context.SaveChanges();

        // Create admin user
        _adminUser = new User
        {
            Email = "admin@test.com",
            PasswordHash = "stub_hash",
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            TenantId = _testTenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(_adminUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Database_ShouldCreateUser_Successfully()
    {
        // Arrange
        if (_context == null || _testTenant == null) return;

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "stub_hash",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = _testTenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("test@example.com");
        savedUser.TenantId.Should().Be(_testTenant.Id);
    }

    [Fact]
    public async Task Database_ShouldFilterByTenant_Correctly()
    {
        // Arrange
        if (_context == null || _testTenant == null) return;

        var tenant2 = new Tenant { Name = "Tenant 2", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Tenants.Add(tenant2);
        await _context.SaveChangesAsync();

        var user1 = new User
        {
            Email = "user1@test.com",
            PasswordHash = "hash",
            FirstName = "User",
            LastName = "One",
            Role = UserRole.Doctor,
            TenantId = _testTenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Email = "user2@test.com",
            PasswordHash = "hash",
            FirstName = "User",
            LastName = "Two",
            Role = UserRole.Doctor,
            TenantId = tenant2.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var tenant1Users = await _context.Users
            .Where(u => u.TenantId == _testTenant.Id)
            .ToListAsync();

        // Assert
        tenant1Users.Should().HaveCount(2); // admin user + user1
        tenant1Users.All(u => u.TenantId == _testTenant.Id).Should().BeTrue();
    }

    [Fact]
    public async Task Database_ShouldSoftDeleteUser_Correctly()
    {
        // Arrange
        if (_context == null) return;

        var user = new User
        {
            Email = "delete@test.com",
            PasswordHash = "hash",
            FirstName = "Delete",
            LastName = "User",
            Role = UserRole.Doctor,
            TenantId = _testTenant!.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var deletedUser = await _context.Users.FindAsync(user.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsActive.Should().BeFalse();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

