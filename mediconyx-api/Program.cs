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

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Captcha service with HttpClient
builder.Services.AddHttpClient<ICaptchaService, RecaptchaService>();

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
