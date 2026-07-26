using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Services.Invitations;

namespace mediconyx_api.Services.Infrastructure;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        IInvitationService invitationService,
        ILogger<TenantProvisioningService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _invitationService = invitationService;
        _logger = logger;
    }

    public async Task<ProvisioningResult> ProvisionTenantAsync(int salesRequestId)
    {
        var result = new ProvisioningResult();

        try
        {
            var salesRequest = await _context.SalesRequests
                .Include(r => r.HospitalProfile)
                .Include(r => r.TechnicalRequirements)
                .Include(r => r.ModuleSelection)
                .FirstOrDefaultAsync(r => r.Id == salesRequestId);

            if (salesRequest == null)
                return Fail(result, "Sales request not found.");

            if (salesRequest.HospitalProfile == null)
                return Fail(result, "Hospital profile is required for provisioning.");

            if (salesRequest.ModuleSelection == null)
                return Fail(result, "Module selection is required for provisioning.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var subdomain = await GenerateUniqueSubdomainAsync(salesRequest.HospitalProfile.HospitalLegalName);

                var tenant = new Tenant
                {
                    Name = salesRequest.HospitalProfile.HospitalLegalName,
                    Subdomain = subdomain,
                    Address = $"{salesRequest.HospitalProfile.AddressLine1}, {salesRequest.HospitalProfile.City}, {salesRequest.HospitalProfile.StateProvince}",
                    ContactEmail = salesRequest.HospitalProfile.PrimaryContactEmail,
                    ContactPhone = salesRequest.HospitalProfile.PrimaryContactPhone,
                    SubscriptionPlan = salesRequest.SubscriptionPlan ?? "Standard",
                    SubscriptionStartDate = salesRequest.ContractStartDate ?? DateTime.UtcNow,
                    SubscriptionExpiresAt = CalculateSubscriptionExpiry(
                        salesRequest.ContractStartDate ?? DateTime.UtcNow,
                        salesRequest.ContractDuration ?? 12),
                    UserLicenses = salesRequest.UserLicenses ?? salesRequest.Seats ?? 10,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                ConfigureModules(tenant, salesRequest.ModuleSelection);
                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

                // Every tenant starts with a main branch (multi-branch, Tier 4.x)
                _context.Branches.Add(new Branch
                {
                    TenantId = tenant.Id,
                    Name = "Main Branch",
                    Code = "MAIN",
                    Address = tenant.Address,
                    Phone = tenant.ContactPhone,
                    Email = tenant.ContactEmail,
                    IsMain = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tenant created: {TenantId}, Subdomain: {Subdomain}", tenant.Id, subdomain);

                // If the primary contact accepted an admin invitation, use the password they
                // chose (no temp password). Otherwise fall back to generating a temp password.
                var adminEmail = salesRequest.HospitalProfile.PrimaryContactEmail;
                var acceptedInvite = await _invitationService.GetAcceptedPipelineInvitationForEmailAsync(adminEmail);

                string? temporaryPassword = null;
                string passwordHash;
                if (acceptedInvite != null && !string.IsNullOrEmpty(acceptedInvite.PasswordHash))
                {
                    passwordHash = acceptedInvite.PasswordHash;
                    _logger.LogInformation("Using accepted invitation {InvitationId} password for admin {Email}", acceptedInvite.Id, adminEmail);
                }
                else
                {
                    temporaryPassword = GenerateSecurePassword();
                    passwordHash = _passwordService.HashPassword(temporaryPassword);
                }

                var adminUser = new User
                {
                    Email = adminEmail,
                    PasswordHash = passwordHash,
                    FirstName = ExtractFirstName(salesRequest.HospitalProfile.PrimaryContactName),
                    LastName = ExtractLastName(salesRequest.HospitalProfile.PrimaryContactName),
                    PhoneNumber = salesRequest.HospitalProfile.PrimaryContactPhone,
                    Role = UserRole.Admin,
                    TenantId = tenant.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                if (acceptedInvite != null)
                    await _invitationService.MarkConsumedAsync(acceptedInvite.Id, adminUser.Id);

                _logger.LogInformation("Admin user created: {UserId}, Email: {Email}", adminUser.Id, adminUser.Email);

                salesRequest.TenantId = tenant.Id;
                salesRequest.Status = SalesRequestStatus.Active;
                salesRequest.ActivatedAt = DateTime.UtcNow;
                salesRequest.UpdatedAt = DateTime.UtcNow;

                _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
                {
                    SalesRequestId = salesRequestId,
                    Type = "Provisioned",
                    Description = $"Tenant provisioned successfully. Subdomain: {subdomain}, Admin user created.",
                    HappenedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = true;
                result.TenantId = tenant.Id;
                result.AdminUserId = adminUser.Id;
                result.Subdomain = subdomain;
                result.AdminEmail = adminUser.Email;
                result.TemporaryPassword = temporaryPassword;
                result.UsedInvitation = acceptedInvite != null;

                _logger.LogInformation(
                    "Tenant provisioning completed for SalesRequest {SalesRequestId}. TenantId: {TenantId}",
                    salesRequestId, tenant.Id);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during tenant provisioning for SalesRequest {SalesRequestId}", salesRequestId);
                return Fail(result, $"Provisioning failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in provisioning for SalesRequest {SalesRequestId}", salesRequestId);
            return Fail(result, $"Fatal error: {ex.Message}");
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static ProvisioningResult Fail(ProvisioningResult result, string message)
    {
        result.Success = false;
        result.ErrorMessage = message;
        return result;
    }

    private async Task<string> GenerateUniqueSubdomainAsync(string hospitalName)
    {
        var baseSubdomain = SanitizeForSubdomain(hospitalName);
        var subdomain = baseSubdomain;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
                subdomain = $"{baseSubdomain}-{GenerateRandomString(4)}";

            if (!await _context.Tenants.AnyAsync(t => t.Subdomain == subdomain))
                return subdomain;
        }

        return $"{baseSubdomain}-{Guid.NewGuid():N}"[..Math.Min(baseSubdomain.Length + 9, 63)];
    }

    private static string SanitizeForSubdomain(string input)
    {
        var sanitized = Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        sanitized = Regex.Replace(sanitized, @"[\s-]+", "-").Trim('-');
        if (sanitized.Length > 53) sanitized = sanitized[..53].TrimEnd('-');
        if (sanitized.Length < 3) sanitized = "tenant-" + Guid.NewGuid().ToString("N")[..6];
        return sanitized;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*";
        var random = new Random();
        var all = upper + lower + digits + symbols;
        var sb = new StringBuilder();
        sb.Append(upper[random.Next(upper.Length)]);
        sb.Append(lower[random.Next(lower.Length)]);
        sb.Append(digits[random.Next(digits.Length)]);
        sb.Append(symbols[random.Next(symbols.Length)]);
        for (int i = 0; i < 12; i++) sb.Append(all[random.Next(all.Length)]);
        return new string(sb.ToString().OrderBy(_ => random.Next()).ToArray());
    }

    private static DateTime CalculateSubscriptionExpiry(DateTime start, int months) => start.AddMonths(months);

    private static void ConfigureModules(Tenant tenant, ModuleSelection m)
    {
        tenant.ModuleOpdEnabled = m.OpdManagement;
        tenant.ModuleIpdEnabled = m.IpdManagement;
        tenant.ModuleEmergencyEnabled = m.EmergencyServices;
        tenant.ModulePharmacyEnabled = m.PharmacyManagement;
        tenant.ModuleLaboratoryEnabled = m.Laboratory;
        tenant.ModuleRadiologyEnabled = m.RadiologyImaging;
        tenant.ModuleBillingEnabled = m.BillingInvoicing;
        tenant.ModuleInventoryEnabled = m.InventoryManagement;
        tenant.ModuleHrPayrollEnabled = m.HrPayroll;
        tenant.ModuleAppointmentEnabled = m.AppointmentScheduling;
        tenant.ModuleTelemedicineEnabled = m.Telemedicine;
        tenant.ModuleAnalyticsEnabled = m.AnalyticsReporting;
    }

    private static string ExtractFirstName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "Admin";
    }

    private static string ExtractLastName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "User";
    }
}
