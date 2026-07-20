using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuthService> _logger;

    private const int RefreshTokenExpiryDays = 7;
    private const int AccessTokenExpiryMinutes = 60;

    public AuthService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        IJwtService jwtService,
        IPermissionService permissionService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var tenantExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email && u.TenantId == request.TenantId);

        if (tenantExists)
            throw new InvalidOperationException("User with this email already exists in this tenant.");

        var tenant = await _context.Tenants.FindAsync(request.TenantId);
        if (tenant == null || !tenant.IsActive)
            throw new InvalidOperationException("Invalid or inactive tenant.");

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
            Role = request.Role,
            TenantId = request.TenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var permissions = await _permissionService.GetPermissionsForUserAsync(user);
        var (accessToken, refreshToken) = GenerateTokenPairWithPermissions(user, permissions);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email} in Tenant {TenantId}", user.Email, user.TenantId);

        return BuildAuthResponse(user, accessToken, refreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Patients must verify their email before logging in.
        if (user.Role == UserRole.Patient && !user.EmailVerified)
            throw new UnauthorizedAccessException("Please verify your email before logging in. Check your inbox for the verification link, or request a new one from the login page.");

        EnsureUserAndTenantActive(user);

        return await IssueTokensAsync(user, "User logged in");
    }

    public async Task<AuthResponse> StaffLoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed staff login attempt for email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (user.Role == UserRole.Patient)
            throw new UnauthorizedAccessException("Staff login not permitted for patient accounts.");

        EnsureUserAndTenantActive(user);

        return await IssueTokensAsync(user, "Staff user logged in");
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (!user.IsActive || !user.Tenant.IsActive)
            throw new UnauthorizedAccessException("User or tenant account is inactive.");

        return await IssueTokensAsync(user, null);
    }

    public async Task LogoutAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void EnsureUserAndTenantActive(User user)
    {
        if (!user.IsActive)
            throw new UnauthorizedAccessException("User account is inactive.");
        if (!user.Tenant.IsActive)
            throw new UnauthorizedAccessException("Tenant account is inactive.");
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, string? logMessage)
    {
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        // Union of permissions from primary role + all additional roles (Tier 1.2)
        var permissions = await _permissionService.GetPermissionsForUserAsync(user);
        var (accessToken, refreshToken) = GenerateTokenPairWithPermissions(user, permissions);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        await _context.SaveChangesAsync();

        if (logMessage != null)
            _logger.LogInformation("{Message}: {Email} in Tenant {TenantId}", logMessage, user.Email, user.TenantId);

        return BuildAuthResponse(user, accessToken, refreshToken);
    }

    private (string AccessToken, string RefreshToken) GenerateTokenPairWithPermissions(User user, IEnumerable<string> permissions)
    {
        var access = _jwtService.GenerateAccessToken(user, permissions);
        var refresh = _jwtService.GenerateRefreshToken();
        return (access, refresh);
    }

    private static AuthResponse BuildAuthResponse(User user, string accessToken, string refreshToken) => new()
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes),
        User = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            TenantId = user.TenantId
        }
    };
}
