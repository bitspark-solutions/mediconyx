using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Common;
using mediconyx_api.Services.Users;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserManagementController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(IUserService userService, ILogger<UserManagementController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("me")]
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
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserListResponse>> GetUsers([FromQuery] UserQueryParams queryParams)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        return Ok(await _userService.GetUsersAsync(tenantId, queryParams));
    }

    [HttpGet("{id}")]
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
    [Authorize(Roles = "Admin")]
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

    [HttpPut("{id}")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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


