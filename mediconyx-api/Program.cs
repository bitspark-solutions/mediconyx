using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using mediconyx_api.Data;
using mediconyx_api.Services;
using mediconyx_api.Models;
using mediconyx_api.Middleware;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var issuer = jwtSettings["Issuer"] ?? "Mediconyx";
var audience = jwtSettings["Audience"] ?? "MediconyxAPI";

builder.Services.AddAuthentication(options =>
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

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Application services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

// Captcha service with HttpClient
builder.Services.AddHttpClient<ICaptchaService, RecaptchaService>();

// Storage service with HttpClient
builder.Services.AddHttpClient<IStorageService, StorageService>();
builder.Services.Configure<StorageServiceOptions>(
    builder.Configuration.GetSection("StorageService"));

// LLM/Embedding Provider Configuration
// Supports: OpenAI, Ollama, Alibaba/DashScope, AWS Bedrock, Azure OpenAI, and any OpenAI-compatible API
var llmConfig = builder.Configuration.GetSection("LLM");
var embeddingConfig = builder.Configuration.GetSection("Embedding");

var llmProvider = llmConfig["Provider"] ?? "OpenAI";
var embeddingProvider = embeddingConfig["Provider"] ?? llmProvider; // Default to same as LLM

// LLM Service (any REST API provider)
builder.Services.AddHttpClient<ILLMService, GenericLlmService>();
builder.Services.Configure<GenericLlmOptions>(llmConfig);

// Embedding Service (any REST API provider)
builder.Services.AddHttpClient<IEmbeddingService, GenericEmbeddingService>();
builder.Services.Configure<GenericEmbeddingOptions>(embeddingConfig);

Console.WriteLine($"🧠 LLM Provider: {llmProvider} ({llmConfig["BaseUrl"]}/{llmConfig["Model"]})");
Console.WriteLine($"📊 Embedding Provider: {embeddingProvider} ({embeddingConfig["BaseUrl"]}/{embeddingConfig["Model"]})");

// LLM Provider Factory (for querying current provider info)
builder.Services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();

// RAG Query service
builder.Services.AddScoped<IRagQueryService, RagQueryService>();

// Rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors("AllowFrontend");
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Run migrations on startup (Development only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            await dbContext.Database.MigrateAsync();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            var masterSection = builder.Configuration.GetSection("MasterUser");
            var masterEmail = masterSection["Email"];
            var masterPassword = masterSection["Password"];
            var masterFirstName = masterSection["FirstName"] ?? "Master";
            var masterLastName = masterSection["LastName"] ?? "User";
            if (!string.IsNullOrWhiteSpace(masterEmail) && !string.IsNullOrWhiteSpace(masterPassword))
            {
                var platformTenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Name == "Platform");
                if (platformTenant == null)
                {
                    platformTenant = new Tenant { Name = "Platform", IsActive = true, CreatedAt = DateTime.UtcNow };
                    dbContext.Tenants.Add(platformTenant);
                    await dbContext.SaveChangesAsync();
                }
                var existingMaster = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == masterEmail.ToLower());
                if (existingMaster == null)
                {
                    var user = new User
                    {
                        Email = masterEmail.ToLowerInvariant(),
                        PasswordHash = passwordService.HashPassword(masterPassword),
                        FirstName = masterFirstName,
                        LastName = masterLastName,
                        Role = UserRole.Master,
                        TenantId = platformTenant.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Users.Add(user);
                    await dbContext.SaveChangesAsync();
                }
                
                // Seed a Sales user for development testing
                if (app.Environment.IsDevelopment())
                {
                    var salesEmail = "sales@mediconyx.local";
                    var existingSales = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == salesEmail);
                    if (existingSales == null)
                    {
                        var salesUser = new User
                        {
                            Email = salesEmail,
                            PasswordHash = passwordService.HashPassword("Sales@123"),
                            FirstName = "Sales",
                            LastName = "User",
                            Role = UserRole.Sales,
                            TenantId = platformTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Users.Add(salesUser);
                        await dbContext.SaveChangesAsync();
                    }
                    
                    // Also seed an Admin user for testing
                    var adminEmail = "admin@mediconyx.local";
                    var existingAdmin = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
                    if (existingAdmin == null)
                    {
                        var adminUser = new User
                        {
                            Email = adminEmail,
                            PasswordHash = passwordService.HashPassword("Admin@123"),
                            FirstName = "Admin",
                            LastName = "User",
                            Role = UserRole.Admin,
                            TenantId = platformTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Users.Add(adminUser);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}

app.Run();
