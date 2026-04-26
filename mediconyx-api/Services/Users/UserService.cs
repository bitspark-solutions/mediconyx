using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Services.Users;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UserService> _logger;

    private const int MinPasswordLength = 6;

    public UserService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ILogger<UserService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<UserDetailDto> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        return MapToDetailDto(user);
    }

    public async Task<UserListResponse> GetUsersAsync(int? callerTenantId, UserQueryParams queryParams)
    {
        var query = _context.Users.Include(u => u.Tenant).AsQueryable();

        if (queryParams.TenantId.HasValue)
            query = query.Where(u => u.TenantId == queryParams.TenantId.Value);
        else if (callerTenantId.HasValue)
            query = query.Where(u => u.TenantId == callerTenantId.Value);

        if (queryParams.Role.HasValue)
            query = query.Where(u => u.Role == queryParams.Role.Value);

        if (queryParams.IsActive.HasValue)
            query = query.Where(u => u.IsActive == queryParams.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var term = queryParams.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        query = queryParams.SortBy?.ToLower() switch
        {
            "email" => queryParams.SortOrder == "asc"
                ? query.OrderBy(u => u.Email)
                : query.OrderByDescending(u => u.Email),
            "lastname" => queryParams.SortOrder == "asc"
                ? query.OrderBy(u => u.LastName)
                : query.OrderByDescending(u => u.LastName),
            _ => queryParams.SortOrder == "asc"
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt)
        };

        var pageSize = Math.Min(queryParams.PageSize, 100);
        var pageNumber = Math.Max(queryParams.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new UserListResponse
        {
            Users = users.Select(MapToDetailDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    public async Task<UserDetailDto> GetUserByIdAsync(int requestedUserId, int callerUserId, UserRole callerRole)
    {
        if (callerUserId != requestedUserId && callerRole != UserRole.Admin)
            throw new UnauthorizedAccessException("You do not have permission to view this user.");

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == requestedUserId)
            ?? throw new KeyNotFoundException($"User {requestedUserId} not found.");

        if (callerRole != UserRole.Admin)
        {
            var caller = await _context.Users.FindAsync(callerUserId);
            if (caller == null || user.TenantId != caller.TenantId)
                throw new UnauthorizedAccessException("You do not have permission to view this user.");
        }

        return MapToDetailDto(user);
    }

    public async Task<UserDetailDto> CreateUserAsync(CreateUserRequest request, int adminUserId)
    {
        ValidateEmail(request.Email);
        ValidatePassword(request.Password);

        var exists = await _context.Users.AnyAsync(u =>
            u.Email.ToLower() == request.Email.ToLower() && u.TenantId == request.TenantId);

        if (exists)
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

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {Email} in Tenant {TenantId} by Admin {AdminId}",
            user.Email, user.TenantId, adminUserId);

        var created = await _context.Users.Include(u => u.Tenant).FirstAsync(u => u.Id == user.Id);
        return MapToDetailDto(created);
    }

    public async Task<UserDetailDto> UpdateUserAsync(int userId, UpdateUserRequest request, int callerUserId, UserRole callerRole)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (callerUserId != userId && callerRole != UserRole.Admin)
            throw new UnauthorizedAccessException("You do not have permission to update this user.");

        if (callerRole != UserRole.Admin && (request.Role.HasValue || request.IsActive.HasValue))
            throw new UnauthorizedAccessException("You do not have permission to change role or active status.");

        if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName;
        if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        if (request.Role.HasValue && callerRole == UserRole.Admin) user.Role = request.Role.Value;
        if (request.IsActive.HasValue && callerRole == UserRole.Admin) user.IsActive = request.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId} by {CallerUserId}", userId, callerUserId);

        return MapToDetailDto(user);
    }

    public async Task DeactivateUserAsync(int userId, int callerUserId)
    {
        if (userId == callerUserId)
            throw new InvalidOperationException("You cannot delete your own account.");

        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deactivated: {UserId} by Admin {AdminId}", userId, callerUserId);
    }

    public async Task<UserDetailDto> ToggleActiveAsync(int userId, int callerUserId)
    {
        if (userId == callerUserId)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} active status toggled to {IsActive} by {CallerUserId}",
            userId, user.IsActive, callerUserId);

        return MapToDetailDto(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        ValidatePassword(request.NewPassword);

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for user: {UserId}", userId);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, int adminUserId)
    {
        var user = await _context.Users.FindAsync(request.UserId)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        ValidatePassword(request.NewPassword);

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset for user: {UserId} by Admin {AdminId}", request.UserId, adminUserId);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void ValidateEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
                throw new ArgumentException("Invalid email format.");
        }
        catch
        {
            throw new ArgumentException("Invalid email format.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            throw new ArgumentException($"Password must be at least {MinPasswordLength} characters long.");
    }

    internal static UserDetailDto MapToDetailDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        TenantId = user.TenantId,
        TenantName = user.Tenant?.Name ?? string.Empty,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        LastLoginAt = user.LastLoginAt
    };
}
