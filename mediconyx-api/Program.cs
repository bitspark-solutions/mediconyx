using System.Text;
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
using mediconyx_api.Services.Patients;
using mediconyx_api.Services.Users;
using mediconyx_api.Models;
using mediconyx_api.Swagger;
using mediconyx_api.Middleware;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Document endpoints that take IFormFile via [FromForm]
    // (FileAttachmentController.Upload, etc.) — Swashbuckle cannot model
    // these out of the box and would 500 /swagger/v1/swagger.json otherwise.
    options.OperationFilter<FileUploadOperationFilter>();
    options.SupportNonNullableReferenceTypes();
});
builder.Services.AddMemoryCache();
builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();

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

// Infrastructure services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
builder.Services.AddHttpClient<ICaptchaService, RecaptchaService>();

// Feature services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

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
builder.Services.AddHttpClient<IEmbeddingService, GenericEmbeddingService>();
builder.Services.Configure<GenericEmbeddingOptions>(embeddingConfig);

Console.WriteLine($"🧠 LLM Provider: {llmProvider} ({llmConfig["BaseUrl"]}/{llmConfig["Model"]})");
Console.WriteLine($"📊 Embedding Provider: {embeddingProvider} ({embeddingConfig["BaseUrl"]}/{embeddingConfig["Model"]})");

// LLM Provider Factory (for querying current provider info)
builder.Services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
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

// HTTPS redirection is only useful on a developer machine where Kestrel
// speaks HTTPS directly. In containerized deployments (regardless of
// ASPNETCORE_ENVIRONMENT value — the dev container also runs as
// "Development") TLS is terminated at the reverse proxy / load balancer
// and the app speaks plain HTTP. Calling UseHttpsRedirection() here
// would only log the noisy "Failed to determine the https port for
// redirect" warning on every request. DOTNET_RUNNING_IN_CONTAINER is
// automatically set by the .NET base images.
var runningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!runningInContainer)
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors("AllowFrontend");
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Run migrations on startup (Development only)
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
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
                if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
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

                    // ── HOSPITAL SEED ─────────────────────────────────────────
                    // Seed a demo hospital tenant: City General Hospital
                    var hospitalTenantName = "City General Hospital";
                    var hospitalTenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Name == hospitalTenantName);
                    if (hospitalTenant == null)
                    {
                        hospitalTenant = new Tenant
                        {
                            Name = hospitalTenantName,
                            Subdomain = "city-general",
                            Address = "123 Medical Drive, Dhaka, BD",
                            ContactEmail = "admin@citygeneral.local",
                            ContactPhone = "+880-1700-000001",
                            SubscriptionPlan = "Standard",
                            SubscriptionStartDate = DateTime.UtcNow,
                            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
                            UserLicenses = 50,
                            IsActive = true,
                            ModuleOpdEnabled = true,
                            ModuleAppointmentEnabled = true,
                            ModuleBillingEnabled = true,
                            ModulePharmacyEnabled = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Tenants.Add(hospitalTenant);
                        await dbContext.SaveChangesAsync();

                        // ── Hospital Admin ─────────────────────────────────────
                        var hospitalAdmin = new User
                        {
                            Email = "admin@citygeneral.local",
                            PasswordHash = passwordService.HashPassword("Hospital@123"),
                            FirstName = "Sarah",
                            LastName = "Ahmed",
                            PhoneNumber = "+880-1700-000001",
                            Role = UserRole.Admin,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Users.Add(hospitalAdmin);
                        await dbContext.SaveChangesAsync();

                        // ── Receptionist ───────────────────────────────────────
                        var receptionist = new User
                        {
                            Email = "reception@citygeneral.local",
                            PasswordHash = passwordService.HashPassword("Reception@123"),
                            FirstName = "Nadia",
                            LastName = "Islam",
                            PhoneNumber = "+880-1700-000005",
                            Role = UserRole.Receptionist,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Users.Add(receptionist);
                        await dbContext.SaveChangesAsync();

                        // ── Departments ────────────────────────────────────────
                        var deptGeneral = new Department { TenantId = hospitalTenant.Id, Name = "General Medicine", Code = "GEN", Description = "General outpatient medicine", IsActive = true, CreatedAt = DateTime.UtcNow };
                        var deptCardio = new Department { TenantId = hospitalTenant.Id, Name = "Cardiology", Code = "CARD", Description = "Heart and cardiovascular care", IsActive = true, CreatedAt = DateTime.UtcNow };
                        var deptPeds = new Department { TenantId = hospitalTenant.Id, Name = "Pediatrics", Code = "PEDS", Description = "Children's healthcare", IsActive = true, CreatedAt = DateTime.UtcNow };
                        dbContext.Departments.AddRange(deptGeneral, deptCardio, deptPeds);
                        await dbContext.SaveChangesAsync();

                        // ── Doctor 1: General Medicine ─────────────────────────
                        var doc1User = new User
                        {
                            Email = "dr.khan@citygeneral.local",
                            PasswordHash = passwordService.HashPassword("Doctor@123"),
                            FirstName = "Arif",
                            LastName = "Khan",
                            PhoneNumber = "+880-1700-000010",
                            Role = UserRole.Doctor,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Users.Add(doc1User);
                        await dbContext.SaveChangesAsync();
                        dbContext.DoctorProfiles.Add(new DoctorProfile
                        {
                            UserId = doc1User.Id,
                            TenantId = hospitalTenant.Id,
                            MedicalLicenseNumber = "BMDC-2015-001",
                            Specialization = MedicalSpecializations.General,
                            DepartmentId = deptGeneral.Id,
                            ConsultationDurationMinutes = 15,
                            ConsultationFee = 500,
                            IsAcceptingPatients = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                        await dbContext.SaveChangesAsync();

                        // ── Doctor 2: Cardiology ───────────────────────────────
                        var doc2User = new User
                        {
                            Email = "dr.rahman@citygeneral.local",
                            PasswordHash = passwordService.HashPassword("Doctor@123"),
                            FirstName = "Fatima",
                            LastName = "Rahman",
                            PhoneNumber = "+880-1700-000011",
                            Role = UserRole.Doctor,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Users.Add(doc2User);
                        await dbContext.SaveChangesAsync();
                        dbContext.DoctorProfiles.Add(new DoctorProfile
                        {
                            UserId = doc2User.Id,
                            TenantId = hospitalTenant.Id,
                            MedicalLicenseNumber = "BMDC-2012-047",
                            Specialization = MedicalSpecializations.Cardiology,
                            DepartmentId = deptCardio.Id,
                            ConsultationDurationMinutes = 30,
                            ConsultationFee = 1200,
                            IsAcceptingPatients = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                        await dbContext.SaveChangesAsync();

                        // ── Doctor 3: Pediatrics ───────────────────────────────
                        var doc3User = new User
                        {
                            Email = "dr.hossain@citygeneral.local",
                            PasswordHash = passwordService.HashPassword("Doctor@123"),
                            FirstName = "Rahim",
                            LastName = "Hossain",
                            PhoneNumber = "+880-1700-000012",
                            Role = UserRole.Doctor,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Users.Add(doc3User);
                        await dbContext.SaveChangesAsync();
                        dbContext.DoctorProfiles.Add(new DoctorProfile
                        {
                            UserId = doc3User.Id,
                            TenantId = hospitalTenant.Id,
                            MedicalLicenseNumber = "BMDC-2018-112",
                            Specialization = MedicalSpecializations.Pediatrics,
                            DepartmentId = deptPeds.Id,
                            ConsultationDurationMinutes = 20,
                            ConsultationFee = 800,
                            IsAcceptingPatients = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                        await dbContext.SaveChangesAsync();

                        // ── Patients ───────────────────────────────────────────
                        var patientUser1 = new User
                        {
                            Email = "karim.hassan@patient.local",
                            PasswordHash = passwordService.HashPassword("Patient@123"),
                            FirstName = "Karim",
                            LastName = "Hassan",
                            PhoneNumber = "+880-1800-000101",
                            Role = UserRole.Patient,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        var patientUser2 = new User
                        {
                            Email = "rania.begum@patient.local",
                            PasswordHash = passwordService.HashPassword("Patient@123"),
                            FirstName = "Rania",
                            LastName = "Begum",
                            PhoneNumber = "+880-1800-000102",
                            Role = UserRole.Patient,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        var patientUser3 = new User
                        {
                            Email = "tariq.miah@patient.local",
                            PasswordHash = passwordService.HashPassword("Patient@123"),
                            FirstName = "Tariq",
                            LastName = "Miah",
                            PhoneNumber = "+880-1800-000103",
                            Role = UserRole.Patient,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        var patientUser4 = new User
                        {
                            Email = "lina.chowdhury@patient.local",
                            PasswordHash = passwordService.HashPassword("Patient@123"),
                            FirstName = "Lina",
                            LastName = "Chowdhury",
                            PhoneNumber = "+880-1800-000104",
                            Role = UserRole.Patient,
                            TenantId = hospitalTenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Users.AddRange(patientUser1, patientUser2, patientUser3, patientUser4);
                        await dbContext.SaveChangesAsync();

                        dbContext.Patients.AddRange(
                            new Patient
                            {
                                TenantId = hospitalTenant.Id,
                                FirstName = "Karim", LastName = "Hassan",
                                DateOfBirth = new DateTime(1985, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                                Gender = Gender.Male,
                                BloodGroup = "B+",
                                Phone = "+880-1800-000101",
                                Email = "karim.hassan@patient.local",
                                Address = "45 Green Road, Dhanmondi",
                                City = "Dhaka",
                                EmergencyContactName = "Fatima Hassan",
                                EmergencyContactPhone = "+880-1800-000109",
                                EmergencyContactRelation = "Spouse",
                                PatientNumber = "P-000001",
                                CreatedByUserId = hospitalAdmin.Id,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            },
                            new Patient
                            {
                                TenantId = hospitalTenant.Id,
                                FirstName = "Rania", LastName = "Begum",
                                DateOfBirth = new DateTime(1992, 7, 22, 0, 0, 0, DateTimeKind.Utc),
                                Gender = Gender.Female,
                                BloodGroup = "O+",
                                Phone = "+880-1800-000102",
                                Email = "rania.begum@patient.local",
                                Address = "12 Mirpur Road, Pallabi",
                                City = "Dhaka",
                                EmergencyContactName = "Jalal Begum",
                                EmergencyContactPhone = "+880-1800-000110",
                                EmergencyContactRelation = "Father",
                                PatientNumber = "P-000002",
                                CreatedByUserId = hospitalAdmin.Id,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            },
                            new Patient
                            {
                                TenantId = hospitalTenant.Id,
                                FirstName = "Tariq", LastName = "Miah",
                                DateOfBirth = new DateTime(1978, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                                Gender = Gender.Male,
                                BloodGroup = "A-",
                                Phone = "+880-1800-000103",
                                Email = "tariq.miah@patient.local",
                                Address = "88 Tejgaon Industrial Area",
                                City = "Dhaka",
                                EmergencyContactName = "Sumaiya Miah",
                                EmergencyContactPhone = "+880-1800-000111",
                                EmergencyContactRelation = "Wife",
                                PatientNumber = "P-000003",
                                CreatedByUserId = hospitalAdmin.Id,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            },
                            new Patient
                            {
                                TenantId = hospitalTenant.Id,
                                FirstName = "Lina", LastName = "Chowdhury",
                                DateOfBirth = new DateTime(2010, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                                Gender = Gender.Female,
                                BloodGroup = "AB+",
                                Phone = "+880-1800-000104",
                                Email = "lina.chowdhury@patient.local",
                                Address = "22 Gulshan Avenue, Gulshan-2",
                                City = "Dhaka",
                                EmergencyContactName = "Rahim Chowdhury",
                                EmergencyContactPhone = "+880-1800-000112",
                                EmergencyContactRelation = "Father",
                                PatientNumber = "P-000004",
                                CreatedByUserId = hospitalAdmin.Id,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            }
                        );
                        await dbContext.SaveChangesAsync();
                    }
                    // ── END HOSPITAL SEED ──────────────────────────────────────
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
