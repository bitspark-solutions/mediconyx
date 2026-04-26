using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Patients;

namespace mediconyx_api.Tests.UnitTests;

public class PatientServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PatientService _sut;
    private const int TenantId = 1;

    public PatientServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new PatientService(_context, Mock.Of<ILogger<PatientService>>());
    }

    private Patient AddPatient(int id, string firstName, string lastName, string patientNumber,
        bool isActive = true, Gender gender = Gender.Male)
    {
        var patient = new Patient
        {
            Id = id,
            TenantId = TenantId,
            FirstName = firstName,
            LastName = lastName,
            PatientNumber = patientNumber,
            Phone = "01700000000",
            Gender = gender,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        _context.Patients.Add(patient);
        _context.SaveChanges();
        return patient;
    }

    // ── GetPatientsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPatientsAsync_ShouldReturnOnlyTenantPatients()
    {
        AddPatient(1, "Alice", "Smith", "P-000001");
        _context.Patients.Add(new Patient
        {
            Id = 2, TenantId = 99, FirstName = "Bob", LastName = "Jones",
            PatientNumber = "P-000001", Phone = "0", Gender = Gender.Male,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetPatientsAsync(TenantId, new PatientQueryParams { PageSize = 20, PageNumber = 1 });

        result.Patients.Should().ContainSingle().Which.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetPatientsAsync_ShouldFilterByActiveStatus()
    {
        AddPatient(1, "Active", "One", "P-000001", isActive: true);
        AddPatient(2, "Inactive", "Two", "P-000002", isActive: false);

        var result = await _sut.GetPatientsAsync(TenantId,
            new PatientQueryParams { IsActive = true, PageSize = 20, PageNumber = 1 });

        result.Patients.Should().ContainSingle().Which.FirstName.Should().Be("Active");
    }

    [Fact]
    public async Task GetPatientsAsync_ShouldFilterByGender()
    {
        AddPatient(1, "Male", "Patient", "P-000001", gender: Gender.Male);
        AddPatient(2, "Female", "Patient", "P-000002", gender: Gender.Female);

        var result = await _sut.GetPatientsAsync(TenantId,
            new PatientQueryParams { Gender = Gender.Female, PageSize = 20, PageNumber = 1 });

        result.Patients.Should().ContainSingle().Which.FirstName.Should().Be("Female");
    }

    [Fact]
    public async Task GetPatientsAsync_ShouldSearchByName()
    {
        AddPatient(1, "Karim", "Hassan", "P-000001");
        AddPatient(2, "Rania", "Begum", "P-000002");

        var result = await _sut.GetPatientsAsync(TenantId,
            new PatientQueryParams { SearchTerm = "karim", PageSize = 20, PageNumber = 1 });

        result.Patients.Should().ContainSingle().Which.FirstName.Should().Be("Karim");
    }

    [Fact]
    public async Task GetPatientsAsync_ShouldPaginateCorrectly()
    {
        for (int i = 1; i <= 5; i++)
            AddPatient(i, $"Patient{i}", "X", $"P-{i:D6}");

        var result = await _sut.GetPatientsAsync(TenantId,
            new PatientQueryParams { PageSize = 2, PageNumber = 2 });

        result.Patients.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
    }

    // ── GetPatientByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPatientByIdAsync_ShouldReturnDto_WhenFound()
    {
        AddPatient(1, "Alice", "Smith", "P-000001");

        var result = await _sut.GetPatientByIdAsync(TenantId, patientId: 1);

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetPatientByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _sut.GetPatientByIdAsync(TenantId, patientId: 999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPatientByIdAsync_ShouldReturnNull_WhenWrongTenant()
    {
        AddPatient(1, "Alice", "Smith", "P-000001");

        var result = await _sut.GetPatientByIdAsync(tenantId: 99, patientId: 1);
        result.Should().BeNull();
    }

    // ── CreatePatientAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreatePatientAsync_ShouldPersistAndReturnDto()
    {
        var request = new CreatePatientRequest
        {
            FirstName = "New",
            LastName = "Patient",
            Phone = "01711111111",
            Gender = Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _sut.CreatePatientAsync(TenantId, createdByUserId: 1, request);

        result.FirstName.Should().Be("New");
        result.PatientNumber.Should().Be("P-000001");
        _context.Patients.Should().ContainSingle();
    }

    [Fact]
    public async Task CreatePatientAsync_ShouldIncrementPatientNumber_WhenPatientsExist()
    {
        AddPatient(1, "Existing", "One", "P-000001");

        var request = new CreatePatientRequest
        {
            FirstName = "Second",
            LastName = "Patient",
            Phone = "01711111111",
            Gender = Gender.Female,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _sut.CreatePatientAsync(TenantId, createdByUserId: 1, request);

        result.PatientNumber.Should().Be("P-000002");
    }

    // ── UpdatePatientAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePatientAsync_ShouldUpdateFields()
    {
        AddPatient(1, "Old", "Name", "P-000001");

        var request = new UpdatePatientRequest { FirstName = "New", LastName = "Name" };
        var result = await _sut.UpdatePatientAsync(TenantId, patientId: 1, request);

        result.FirstName.Should().Be("New");
    }

    [Fact]
    public async Task UpdatePatientAsync_ShouldThrow_WhenNotFound()
    {
        await _sut.Invoking(s => s.UpdatePatientAsync(TenantId, 999, new UpdatePatientRequest()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdatePatientAsync_ShouldThrow_WhenWrongTenant()
    {
        AddPatient(1, "Alice", "Smith", "P-000001");

        await _sut.Invoking(s => s.UpdatePatientAsync(tenantId: 99, patientId: 1, new UpdatePatientRequest()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeactivatePatientAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeactivatePatientAsync_ShouldSetInactive()
    {
        AddPatient(1, "Alice", "Smith", "P-000001", isActive: true);

        await _sut.DeactivatePatientAsync(TenantId, patientId: 1);

        var patient = await _context.Patients.FindAsync(1);
        patient!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivatePatientAsync_ShouldThrow_WhenNotFound()
    {
        await _sut.Invoking(s => s.DeactivatePatientAsync(TenantId, 999))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
