using Microsoft.EntityFrameworkCore;
using mediconyx_api.Models;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Data.Seeding;

/// <summary>
/// Seeds the demo hospital tenant (City General Hospital) with its admin,
/// receptionist, departments, doctors (+profiles) and patients (+users).
/// Runs only when the tenant does not yet exist.
/// </summary>
public static class DemoHospitalSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, IPasswordService passwordService)
    {
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
    }
}
