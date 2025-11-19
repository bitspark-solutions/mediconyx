using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;

namespace mediconyx_api.Services;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ILogger<TenantProvisioningService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<ProvisioningResult> ProvisionTenantAsync(int salesRequestId)
    {
        var result = new ProvisioningResult();

        try
        {
            // Load sales request with all related data
            var salesRequest = await _context.SalesRequests
                .Include(r => r.HospitalProfile)
                .Include(r => r.TechnicalRequirements)
                .Include(r => r.ModuleSelection)
                .FirstOrDefaultAsync(r => r.Id == salesRequestId);

            if (salesRequest == null)
            {
                result.Success = false;
                result.ErrorMessage = "Sales request not found.";
                return result;
            }

            if (salesRequest.HospitalProfile == null)
            {
                result.Success = false;
                result.ErrorMessage = "Hospital profile is required for provisioning.";
                return result;
            }

            if (salesRequest.ModuleSelection == null)
            {
                result.Success = false;
                result.ErrorMessage = "Module selection is required for provisioning.";
                return result;
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Generate unique subdomain
                var subdomain = await GenerateUniqueSubdomainAsync(salesRequest.HospitalProfile.HospitalLegalName);
                
                // Step 2: Create Tenant
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

                // Configure enabled modules from ModuleSelection
                ConfigureModules(tenant, salesRequest.ModuleSelection);

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tenant created: {TenantId}, Subdomain: {Subdomain}", tenant.Id, subdomain);

                // Step 3: Generate secure temporary password
                var temporaryPassword = GenerateSecurePassword();

                // Step 4: Create Admin User
                var adminUser = new User
                {
                    Email = salesRequest.HospitalProfile.PrimaryContactEmail,
                    PasswordHash = _passwordService.HashPassword(temporaryPassword),
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

                _logger.LogInformation("Admin user created: {UserId}, Email: {Email}", adminUser.Id, adminUser.Email);

                // Step 5: Link Tenant to SalesRequest
                salesRequest.TenantId = tenant.Id;
                salesRequest.Status = SalesRequestStatus.Active;
                salesRequest.ActivatedAt = DateTime.UtcNow;
                salesRequest.UpdatedAt = DateTime.UtcNow;

                // Step 6: Create timeline event
                _context.OnboardingTimelineEvents.Add(new OnboardingTimelineEvent
                {
                    SalesRequestId = salesRequestId,
                    Type = "Provisioned",
                    Description = $"Tenant provisioned successfully. Subdomain: {subdomain}, Admin user created.",
                    HappenedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                // Success result
                result.Success = true;
                result.TenantId = tenant.Id;
                result.AdminUserId = adminUser.Id;
                result.Subdomain = subdomain;
                result.AdminEmail = adminUser.Email;
                result.TemporaryPassword = temporaryPassword;

                _logger.LogInformation(
                    "Tenant provisioning completed successfully for SalesRequest {SalesRequestId}. TenantId: {TenantId}",
                    salesRequestId,
                    tenant.Id);

                return result;
            }
            catch (Exception ex)
            {
                // Rollback transaction on any error
                await transaction.RollbackAsync();
                
                _logger.LogError(ex, "Error during tenant provisioning for SalesRequest {SalesRequestId}", salesRequestId);
                
                result.Success = false;
                result.ErrorMessage = $"Provisioning failed: {ex.Message}";
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in provisioning process for SalesRequest {SalesRequestId}", salesRequestId);
            
            result.Success = false;
            result.ErrorMessage = $"Fatal error: {ex.Message}";
            return result;
        }
    }

    private async Task<string> GenerateUniqueSubdomainAsync(string hospitalName)
    {
        var baseSubdomain = SanitizeForSubdomain(hospitalName);
        var subdomain = baseSubdomain;
        var attempt = 0;
        const int maxAttempts = 3;

        while (attempt < maxAttempts)
        {
            // Add random suffix if not first attempt
            if (attempt > 0)
            {
                var randomSuffix = GenerateRandomString(4);
                subdomain = $"{baseSubdomain}-{randomSuffix}";
            }

            // Check uniqueness
            var exists = await _context.Tenants
                .AnyAsync(t => t.Subdomain == subdomain);

            if (!exists)
            {
                return subdomain;
            }

            attempt++;
        }

        // If all attempts failed, use GUID suffix
        var guidSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{baseSubdomain}-{guidSuffix}";
    }

    private string SanitizeForSubdomain(string input)
    {
        // Convert to lowercase
        var sanitized = input.ToLowerInvariant();

        // Remove non-alphanumeric characters except hyphens
        sanitized = Regex.Replace(sanitized, @"[^a-z0-9\s-]", "");

        // Replace spaces and multiple hyphens with single hyphen
        sanitized = Regex.Replace(sanitized, @"[\s-]+", "-");

        // Remove leading/trailing hyphens
        sanitized = sanitized.Trim('-');

        // Limit length to 63 characters (DNS subdomain limit)
        if (sanitized.Length > 53)
        {
            sanitized = sanitized.Substring(0, 53).TrimEnd('-');
        }

        // Ensure minimum length of 3
        if (sanitized.Length < 3)
        {
            sanitized = "tenant-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        return sanitized;
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string GenerateSecurePassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*";

        var random = new Random();
        var password = new StringBuilder();

        // Ensure at least one character from each category
        password.Append(uppercase[random.Next(uppercase.Length)]);
        password.Append(lowercase[random.Next(lowercase.Length)]);
        password.Append(digits[random.Next(digits.Length)]);
        password.Append(symbols[random.Next(symbols.Length)]);

        // Fill remaining 12 characters randomly
        var allChars = uppercase + lowercase + digits + symbols;
        for (int i = 0; i < 12; i++)
        {
            password.Append(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the password
        return new string(password.ToString().ToCharArray().OrderBy(x => random.Next()).ToArray());
    }

    private DateTime CalculateSubscriptionExpiry(DateTime startDate, int durationMonths)
    {
        return startDate.AddMonths(durationMonths);
    }

    private void ConfigureModules(Tenant tenant, ModuleSelection moduleSelection)
    {
        tenant.ModuleOpdEnabled = moduleSelection.OpdManagement;
        tenant.ModuleIpdEnabled = moduleSelection.IpdManagement;
        tenant.ModuleEmergencyEnabled = moduleSelection.EmergencyServices;
        tenant.ModulePharmacyEnabled = moduleSelection.PharmacyManagement;
        tenant.ModuleLaboratoryEnabled = moduleSelection.Laboratory;
        tenant.ModuleRadiologyEnabled = moduleSelection.RadiologyImaging;
        tenant.ModuleBillingEnabled = moduleSelection.BillingInvoicing;
        tenant.ModuleInventoryEnabled = moduleSelection.InventoryManagement;
        tenant.ModuleHrPayrollEnabled = moduleSelection.HrPayroll;
        tenant.ModuleAppointmentEnabled = moduleSelection.AppointmentScheduling;
        tenant.ModuleTelemedicineEnabled = moduleSelection.Telemedicine;
        tenant.ModuleAnalyticsEnabled = moduleSelection.AnalyticsReporting;
    }

    private string ExtractFirstName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "Admin";
    }

    private string ExtractLastName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "User";
    }
}
