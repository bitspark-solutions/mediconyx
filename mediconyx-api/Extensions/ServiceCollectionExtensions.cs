using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using mediconyx_api.Data;
using mediconyx_api.Services.AI;
using mediconyx_api.Services.Auth;
using mediconyx_api.Services.Appointments;
using mediconyx_api.Services.Infrastructure;
using mediconyx_api.Services.Invoices;
using mediconyx_api.Services.Inpatient;
using mediconyx_api.Services.Patients;
using mediconyx_api.Services.Users;
using mediconyx_api.Models;
using mediconyx_api.Swagger;
using AspNetCoreRateLimit;

namespace mediconyx_api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediconyxServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Add services to the container
        // JsonStringEnumConverter: the UI sends enums as strings ("Male", "Doctor");
        // numeric values remain accepted for backwards compatibility.
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Document endpoints that take IFormFile via [FromForm]
            // (FileAttachmentController.Upload, etc.) — Swashbuckle cannot model
            // these out of the box and would 500 /swagger/v1/swagger.json otherwise.
            options.OperationFilter<FileUploadOperationFilter>();
            options.SupportNonNullableReferenceTypes();
        });
        services.AddMemoryCache();
        services.AddDataProtection()
            .UseEphemeralDataProtectionProvider();

        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = jwtSettings["Issuer"] ?? "Mediconyx";
        var audience = jwtSettings["Audience"] ?? "MediconyxAPI";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });

        services.AddAuthorization();

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                var frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:3000";
                policy.WithOrigins(frontendUrl)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // Infrastructure services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IFileValidationService, FileValidationService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddHttpClient<ICaptchaService, RecaptchaService>();

        // Feature services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IBedBillingService, BedBillingService>();
        services.AddScoped<IPatientSelfRegistrationService, PatientSelfRegistrationService>();

        // Storage service with HttpClient
        services.AddHttpClient<IStorageService, StorageService>();
        services.Configure<StorageServiceOptions>(
            configuration.GetSection("StorageService"));

        // LLM/Embedding Provider Configuration
        // Supports: OpenAI, Ollama, Alibaba/DashScope, AWS Bedrock, Azure OpenAI, and any OpenAI-compatible API
        var llmConfig = configuration.GetSection("LLM");
        var embeddingConfig = configuration.GetSection("Embedding");

        var llmProvider = llmConfig["Provider"] ?? "OpenAI";
        var embeddingProvider = embeddingConfig["Provider"] ?? llmProvider; // Default to same as LLM

        // LLM Service (any REST API provider)
        services.AddHttpClient<ILLMService, GenericLlmService>();
        services.Configure<GenericLlmOptions>(llmConfig);
        services.AddHttpClient<IEmbeddingService, GenericEmbeddingService>();
        services.Configure<GenericEmbeddingOptions>(embeddingConfig);

        Console.WriteLine($"🧠 LLM Provider: {llmProvider} ({llmConfig["BaseUrl"]}/{llmConfig["Model"]})");
        Console.WriteLine($"📊 Embedding Provider: {embeddingProvider} ({embeddingConfig["BaseUrl"]}/{embeddingConfig["Model"]})");

        // LLM Provider Factory (for querying current provider info)
        services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
        services.AddScoped<IRagQueryService, RagQueryService>();

        // RBAC (Tier 1.1) — permission service + per-permission policies + handler
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        foreach (var (name, _, _) in PermissionCatalog.All)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(name, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new HasPermissionRequirement(name));
                });
            });
        }

        // Rate limiting
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }
}
