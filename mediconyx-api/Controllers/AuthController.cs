using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Auth;
using mediconyx_api.Services.Common;
using System.Security.Claims;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPatientSelfRegistrationService _selfReg;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IPatientSelfRegistrationService selfReg,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _selfReg = selfReg;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("register-patient")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterPatientResponse>> RegisterPatient(RegisterPatientRequest request)
    {
        try
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _selfReg.RegisterAsync(request, remoteIp);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmail([FromQuery] string token)
    {
        var result = await _selfReg.VerifyEmailAsync(token);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var ok = await _selfReg.ResendVerificationAsync(request.Email);
        if (!ok) return BadRequest(new { message = "Email not found or already verified." });
        return Ok(new { message = "Verification email sent." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("staff/login")]
    public async Task<ActionResult<AuthResponse>> StaffLogin(LoginRequest request)
    {
        try
        {
            var result = await _authService.StaffLoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = ClaimsHelper.GetUserId(User);
        if (userId == null) return Unauthorized();

        await _authService.LogoutAsync(userId.Value);
        return Ok(new { message = "Logged out." });
    }
}
