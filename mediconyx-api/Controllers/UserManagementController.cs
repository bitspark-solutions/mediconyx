using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services;
using System.Security.Claims;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ILogger<UserManagementController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDetailDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(MapToUserDetailDto(user));
    }

    /// <summary>
    /// Get all users with filtering and pagination (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserListResponse>> GetUsers([FromQuery] UserQueryParams queryParams)
    {
        var currentUserTenantId = GetCurrentTenantId();
        var query = _context.Users.Include(u => u.Tenant).AsQueryable();

        // Filter by tenant (if specified, or use current user's tenant for non-super-admin)
        if (queryParams.TenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == queryParams.TenantId.Value);
        }
        else if (currentUserTenantId.HasValue)
        {
            // If no tenant specified, filter by current user's tenant
            query = query.Where(u => u.TenantId == currentUserTenantId.Value);
        }

        // Filter by role
        if (queryParams.Role.HasValue)
        {
            query = query.Where(u => u.Role == queryParams.Role.Value);
        }

        // Filter by active status
        if (queryParams.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == queryParams.IsActive.Value);
        }

        // Search term (email, first name, last name)
        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var searchTerm = queryParams.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchTerm) ||
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Sorting
        query = queryParams.SortBy?.ToLower() switch
        {
            "email" => queryParams.SortOrder == "asc"
                ? query.OrderBy(u => u.Email)
                : query.OrderByDescending(u => u.Email),
            "lastname" => queryParams.SortOrder == "asc"
                ? query.OrderBy(u => u.LastName)
                : query.OrderByDescending(u => u.LastName),
            "createdat" or _ => queryParams.SortOrder == "asc"
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt)
        };

        // Pagination
        var pageSize = Math.Min(queryParams.PageSize, 100); // Max 100 per page
        var pageNumber = Math.Max(queryParams.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new UserListResponse
        {
            Users = users.Select(MapToUserDetailDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return Ok(response);
    }

    /// <summary>
    /// Get user by ID (Admin only, or own profile)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailDto>> GetUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        // Users can only view their own profile unless they're Admin
        if (currentUserId != id && currentUserRole != UserRole.Admin)
        {
            return StatusCode(403, new { message = "You do not have permission to view this user." });
        }

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Non-admin users can only view users from their own tenant
        if (currentUserRole != UserRole.Admin)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || user.TenantId != currentUser.TenantId)
            {
                return StatusCode(403, new { message = "You do not have permission to view this user." });
            }
        }

        return Ok(MapToUserDetailDto(user));
    }

    /// <summary>
    /// Create a new user (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDetailDto>> CreateUser(CreateUserRequest request)
    {
        // Validate email format
        if (!IsValidEmail(request.Email))
        {
            return BadRequest(new { message = "Invalid email format." });
        }

        // Check if user already exists in tenant
        if (await _context.Users.AnyAsync(u =>
            u.Email.ToLower() == request.Email.ToLower() &&
            u.TenantId == request.TenantId))
        {
            return BadRequest(new { message = "User with this email already exists in this tenant." });
        }

        // Verify tenant exists and is active
        var tenant = await _context.Tenants.FindAsync(request.TenantId);
        if (tenant == null || !tenant.IsActive)
        {
            return BadRequest(new { message = "Invalid or inactive tenant." });
        }

        // Validate password strength
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters long." });
        }

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
            user.Email, user.TenantId, GetCurrentUserId());

        var createdUser = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, MapToUserDetailDto(createdUser!));
    }

    /// <summary>
    /// Update user (Admin can update any user, users can update their own profile)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDetailDto>> UpdateUser(int id, UpdateUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Authorization: Users can only update their own profile unless they're Admin
        if (currentUserId != id && currentUserRole != UserRole.Admin)
        {
            return StatusCode(403, new { message = "You do not have permission to update this user." });
        }

        // Non-admin users cannot change role or active status
        if (currentUserRole != UserRole.Admin)
        {
            if (request.Role.HasValue || request.IsActive.HasValue)
            {
                return StatusCode(403, new { message = "You do not have permission to change role or active status." });
            }
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            user.FirstName = request.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            user.LastName = request.LastName;
        }

        if (request.PhoneNumber != null)
        {
            user.PhoneNumber = request.PhoneNumber;
        }

        if (request.Role.HasValue && currentUserRole == UserRole.Admin)
        {
            user.Role = request.Role.Value;
        }

        if (request.IsActive.HasValue && currentUserRole == UserRole.Admin)
        {
            user.IsActive = request.IsActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId} by {UpdatedBy}", id, currentUserId);

        return Ok(MapToUserDetailDto(user));
    }

    /// <summary>
    /// Delete user (Admin only, soft delete by setting IsActive = false)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Prevent deleting yourself
        if (user.Id == GetCurrentUserId())
        {
            return BadRequest(new { message = "You cannot delete your own account." });
        }

        // Soft delete
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User deactivated: {UserId} by Admin {AdminId}", id, GetCurrentUserId());

        return NoContent();
    }

    /// <summary>
    /// Activate/Deactivate user (Admin only)
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDetailDto>> ToggleUserActive(int id)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Prevent deactivating yourself
        if (user.Id == GetCurrentUserId())
        {
            return BadRequest(new { message = "You cannot deactivate your own account." });
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} active status changed to {IsActive} by Admin {AdminId}",
            id, user.IsActive, GetCurrentUserId());

        return Ok(MapToUserDetailDto(user));
    }

    /// <summary>
    /// Change own password
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Verify current password
        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Current password is incorrect." });
        }

        // Validate new password
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "New password must be at least 6 characters long." });
        }

        // Update password
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for user: {UserId}", userId);

        return Ok(new { message = "Password changed successfully." });
    }

    /// <summary>
    /// Reset user password (Admin only)
    /// </summary>
    [HttpPost("reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Validate new password
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "New password must be at least 6 characters long." });
        }

        // Update password
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset for user: {UserId} by Admin {AdminId}",
            request.UserId, GetCurrentUserId());

        return Ok(new { message = "Password reset successfully." });
    }

    // Helper methods
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                          User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private UserRole GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return role;
        }
        return UserRole.Patient; // Default to least privileged
    }

    private int? GetCurrentTenantId()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        return int.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    private static UserDetailDto MapToUserDetailDto(User user)
    {
        return new UserDetailDto
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

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

