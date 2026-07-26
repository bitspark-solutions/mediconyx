using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Middleware;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Common;
using mediconyx_api.Services.Invitations;
using mediconyx_api.Services.Users;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserManagementController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(IUserService userService, IInvitationService invitationService, ILogger<UserManagementController> logger)
    {
        _userService = userService;
        _invitationService = invitationService;
        _logger = logger;
    }

    public class InviteUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Receptionist;
    }

    [HttpGet("me")]
    [HasPermission(PermissionCatalog.USER_EDIT_OWN)]
    public async Task<ActionResult<UserDetailDto>> GetCurrentUser()
    {
        var userId = ClaimsHelper.GetUserId(User);
        if (userId == null) return Unauthorized();
        try
        {
            return Ok(await _userService.GetCurrentUserAsync(userId.Value));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "User not found." }); }
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.USER_VIEW)]
    public async Task<ActionResult<UserListResponse>> GetUsers([FromQuery] UserQueryParams queryParams)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        return Ok(await _userService.GetUsersAsync(tenantId, queryParams));
    }

    [HttpGet("{id}")]
    [HasPermission(PermissionCatalog.USER_VIEW)]
    public async Task<ActionResult<UserDetailDto>> GetUser(int id)
    {
        var currentUserId = ClaimsHelper.GetUserId(User);
        var currentUserRole = ClaimsHelper.GetRole(User);
        if (currentUserId == null) return Unauthorized();

        try
        {
            return Ok(await _userService.GetUserByIdAsync(id, currentUserId.Value, currentUserRole));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "User not found." }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.USER_MANAGE)]
    public async Task<ActionResult<UserDetailDto>> CreateUser(CreateUserRequest request)
    {
        var currentUserId = ClaimsHelper.GetUserId(User);
        if (currentUserId == null) return Unauthorized();

        try
        {
            var user = await _userService.CreateUserAsync(request, currentUserId.Value);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("invite")]
    [HasPermission(PermissionCatalog.USER_MANAGE)]
    public async Task<IActionResult> InviteUser(InviteUserRequest request, CancellationToken ct)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        var currentUserId = ClaimsHelper.GetUserId(User);
        if (tenantId == null || currentUserId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        try
        {
            var invitation = await _invitationService.CreateTenantInvitationAsync(
                tenantId.Value, request.Email, request.Role, currentUserId.Value, ct);
            return Ok(new
            {
                message = $"Invitation sent to {invitation.Email}.",
                invitationId = invitation.Id,
                email = invitation.Email,
                role = invitation.IntendedRole.ToString(),
                status = invitation.Status.ToString()
            });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    [HasPermission(PermissionCatalog.USER_MANAGE)]
    public async Task<ActionResult<UserDetailDto>> UpdateUser(int id, UpdateUserRequest request)
    {
        var currentUserId = ClaimsHelper.GetUserId(User);
        var currentUserRole = ClaimsHelper.GetRole(User);
        if (currentUserId == null) return Unauthorized();

        try
        {
            return Ok(await _userService.UpdateUserAsync(id, request, currentUserId.Value, currentUserRole));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "User not found." }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    [HasPermission(PermissionCatalog.USER_MANAGE)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = ClaimsHelper.GetUserId(User);
        if (currentUserId == null) return Unauthorized();

        try
        {
            await _userService.DeactivateUserAsync(id, currentUserId.Value);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "User not found." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/toggle-active")]
    [HasPermission(PermissionCatalog.USER_MANAGE)]
    public async Task<ActionResult<UserDetailDto>> ToggleUserActive(int id)
    {
        var currentUserId = ClaimsHelper.GetUserId(User);
        if (currentUserId == null) return Unauthorized();

        try
        {
            return Ok(await _userService.ToggleActiveAsync(id, currentUserId.Value));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "User not found." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("change-password")]
    [HasPermission(PermissionCatalog.USER_EDIT_OWN)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = ClaimsHelper.GetUserId(User);
        if (userId == null) return Unauthorized();

        try
        {
            await _userService.ChangePasswordAsync(userId.Value, request);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "User not found." }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("reset-password")]
    [HasPermission(PermissionCatalog.USER_MANAGE)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        try
        {
            var adminId = ClaimsHelper.GetUserId(User) ?? 0;
            await _userService.ResetPasswordAsync(request, adminId);
            return Ok(new { message = "Password reset successfully." });
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "User not found." }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}


