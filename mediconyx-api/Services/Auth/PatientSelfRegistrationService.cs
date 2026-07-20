using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Services.Auth;

public interface IPatientSelfRegistrationService
{
    Task<RegisterPatientResponse> RegisterAsync(RegisterPatientRequest request, string? remoteIp, CancellationToken ct = default);
    Task<VerifyEmailResponse> VerifyEmailAsync(string token, CancellationToken ct = default);
    Task<bool> ResendVerificationAsync(string email, CancellationToken ct = default);
}

public class PatientSelfRegistrationService : IPatientSelfRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly IPermissionService _permissionService;
    private readonly IEmailService _emailService;
    private readonly ICaptchaService _captcha;
    private readonly IConfiguration _config;
    private readonly ILogger<PatientSelfRegistrationService> _logger;

    // Verification token expires 48h after issue
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(48);

    // Public URL the user clicks in the verification email.
    // Falls back to a sane localhost default.
    private const string DefaultPublicBaseUrl = "http://localhost:9673";

    public PatientSelfRegistrationService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        IJwtService jwtService,
        IPermissionService permissionService,
        IEmailService emailService,
        ICaptchaService captcha,
        IConfiguration config,
        ILogger<PatientSelfRegistrationService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _permissionService = permissionService;
        _emailService = emailService;
        _captcha = captcha;
        _config = config;
        _logger = logger;
    }

    public async Task<RegisterPatientResponse> RegisterAsync(RegisterPatientRequest request, string? remoteIp, CancellationToken ct = default)
    {
        // 1. Captcha
        var captchaOk = await _captcha.VerifyTokenAsync(request.CaptchaToken ?? string.Empty, remoteIp);
        if (!captchaOk) throw new InvalidOperationException("Captcha verification failed. Please try again.");

        var email = request.Email.Trim().ToLowerInvariant();

        // 2. Resolve tenant
        var tenant = await ResolveTenantAsync(request, ct);
        if (tenant == null) throw new InvalidOperationException("Could not find a hospital with that subdomain/code. Please check the URL or leave it blank to register on the Mediconyx platform.");

        // 3. Email uniqueness — across ALL tenants to prevent duplicate identities.
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == email, ct);
        if (emailTaken) throw new InvalidOperationException("An account with this email already exists. Please log in or use a different email.");

        // 4. Generate verification token
        var token = GenerateToken();

        // 5. Create the user (inactive until verified)
        var user = new User
        {
            Email = email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.Phone.Trim(),
            Role = UserRole.Patient,
            TenantId = tenant.Id,
            IsActive = false,
            EmailVerified = false,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.Add(VerificationTokenLifetime),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        // 6. Create the patient record (minimal — can be enriched later)
        var patientNumber = await GeneratePatientNumberAsync(tenant.Id, ct);
        var patient = new Patient
        {
            TenantId = tenant.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = request.DateOfBirth,
            Gender = ParseGender(request.Gender),
            Phone = user.PhoneNumber,
            Email = user.Email,
            PatientNumber = patientNumber,
            IsActive = true,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync(ct);

        // 7. Send verification email (best-effort; don't fail registration if SMTP is down)
        try
        {
            var baseUrl = _config["App:PublicBaseUrl"] ?? DefaultPublicBaseUrl;
            var link = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
            var body = $@"Hi {user.FirstName},

Welcome to {tenant.Name} on Mediconyx! Please verify your email address by clicking the link below:

{link}

This link expires in 48 hours. If you didn't register for this account, please ignore this email.

— The {tenant.Name} team";
            await _emailService.SendEmailAsync(user.Email, $"Verify your {tenant.Name} account", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}; user must request a resend.", user.Email);
        }

        _logger.LogInformation("Patient self-registered: {Email} in tenant {TenantId} ({TenantName})", user.Email, tenant.Id, tenant.Name);

        return new RegisterPatientResponse
        {
            Message = "Registration successful! Please check your email to verify your account before logging in.",
            Email = user.Email,
            TenantName = tenant.Name,
            RequiresEmailVerification = true
        };
    }

    public async Task<VerifyEmailResponse> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new VerifyEmailResponse { Success = false, Message = "Missing verification token." };

        var user = await _context.Users.Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, ct);
        if (user == null)
            return new VerifyEmailResponse { Success = false, Message = "Invalid or expired verification link." };

        if (user.EmailVerified)
            return new VerifyEmailResponse
            {
                Success = true,
                Message = "Email already verified. You can log in.",
                Email = user.Email,
                TenantName = user.Tenant?.Name
            };

        if (user.EmailVerificationTokenExpiry.HasValue && user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            return new VerifyEmailResponse { Success = false, Message = "Verification link has expired. Please request a new one." };

        user.EmailVerified = true;
        user.IsActive = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Email verified for user {Email} (tenant {TenantId})", user.Email, user.TenantId);

        // Auto-issue tokens so the user is logged in immediately
        var permissions = await _permissionService.GetPermissionsForUserAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, permissions);
        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(ct);

        return new VerifyEmailResponse
        {
            Success = true,
            Message = "Email verified! You are now logged in.",
            Email = user.Email,
            TenantName = user.Tenant?.Name,
            AuthResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    TenantId = user.TenantId
                }
            }
        };
    }

    public async Task<bool> ResendVerificationAsync(string email, CancellationToken ct = default)
    {
        var user = await _context.Users.Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);
        if (user == null || user.EmailVerified) return false;

        user.EmailVerificationToken = GenerateToken();
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.Add(VerificationTokenLifetime);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        try
        {
            var baseUrl = _config["App:PublicBaseUrl"] ?? DefaultPublicBaseUrl;
            var link = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(user.EmailVerificationToken)}";
            var body = $@"Hi {user.FirstName},

You (or someone using your email) requested a new verification link for your {user.Tenant?.Name} account on Mediconyx.

Click here to verify: {link}

This link expires in 48 hours.

— The {user.Tenant?.Name} team";
            await _emailService.SendEmailAsync(user.Email, $"Verify your {user.Tenant?.Name} account", body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);
            return false;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task<Tenant?> ResolveTenantAsync(RegisterPatientRequest request, CancellationToken ct)
    {
        // Priority 1: explicit tenant subdomain (e.g. "citygeneral")
        if (!string.IsNullOrWhiteSpace(request.TenantSubdomain))
        {
            var sub = request.TenantSubdomain.Trim().ToLowerInvariant();
            var t = await _context.Tenants.FirstOrDefaultAsync(x => x.Subdomain == sub && x.IsActive, ct);
            if (t != null) return t;
        }

        // Priority 2: registration code (currently maps to subdomain; reserved for future "hospital code" feature)
        if (!string.IsNullOrWhiteSpace(request.RegistrationCode))
        {
            var code = request.RegistrationCode.Trim().ToLowerInvariant();
            var t = await _context.Tenants.FirstOrDefaultAsync(x => x.Subdomain == code && x.IsActive, ct);
            if (t != null) return t;
        }

        // Priority 3: platform tenant (id=1) — the global "Mediconyx Direct" catch-all
        return await _context.Tenants.FirstOrDefaultAsync(t => t.Id == 1 && t.IsActive, ct);
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        // URL-safe Base64 (no padding)
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static Gender ParseGender(string s) => s?.Trim().ToLowerInvariant() switch
    {
        "male" or "m" or "0" => Gender.Male,
        "female" or "f" or "1" => Gender.Female,
        _ => Gender.Other
    };

    private async Task<string> GeneratePatientNumberAsync(int tenantId, CancellationToken ct)
    {
        // Find the max numeric suffix in this tenant
        var maxNumber = await _context.Patients
            .Where(p => p.TenantId == tenantId && p.PatientNumber.StartsWith("P-"))
            .Select(p => p.PatientNumber)
            .ToListAsync(ct);
        var max = maxNumber
            .Select(n => int.TryParse(n.Substring(2), out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"P-{(max + 1):D6}";
    }
}
