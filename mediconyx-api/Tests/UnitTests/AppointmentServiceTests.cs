using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Appointments;

namespace mediconyx_api.Tests.UnitTests;

public class AppointmentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AppointmentService _sut;
    private const int TenantId = 1;

    public AppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Branches.Add(new Branch { Id = 1, TenantId = TenantId, Name = "Main Branch", Code = "MAIN", IsMain = true });
        _context.SaveChanges();
        _sut = new AppointmentService(_context, Mock.Of<ILogger<AppointmentService>>());
    }

    private Appointment AddAppointment(int id, AppointmentStatus status = AppointmentStatus.Scheduled,
        int patientId = 1, int doctorId = 10)
    {
        var appt = new Appointment
        {
            Id = id,
            TenantId = TenantId,
            PatientId = patientId,
            DoctorId = doctorId,
            DepartmentId = 1,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)),
            Status = status,
            Type = AppointmentType.Consultation,
            CreatedAt = DateTime.UtcNow
        };
        _context.Appointments.Add(appt);
        _context.SaveChanges();
        return appt;
    }

    // ── GetAppointmentsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAppointmentsAsync_ShouldReturnOnlyTenantAppointments()
    {
        AddAppointment(1);
        _context.Appointments.Add(new Appointment
        {
            Id = 2, TenantId = 99, PatientId = 1, DoctorId = 10, DepartmentId = 1,
            AppointmentDate = DateTime.UtcNow.Date, StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11), Status = AppointmentStatus.Scheduled,
            Type = AppointmentType.Consultation, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetAppointmentsAsync(TenantId,
            new AppointmentQueryParams { PageSize = 20, PageNumber = 1 });

        result.Appointments.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetAppointmentsAsync_ShouldFilterByStatus()
    {
        AddAppointment(1, AppointmentStatus.Scheduled);
        AddAppointment(2, AppointmentStatus.Completed);

        var result = await _sut.GetAppointmentsAsync(TenantId, new AppointmentQueryParams
        {
            Status = AppointmentStatus.Completed, PageSize = 20, PageNumber = 1
        });

        result.Appointments.Should().ContainSingle().Which.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetAppointmentsAsync_ShouldFilterByPatientId()
    {
        AddAppointment(1, patientId: 1);
        AddAppointment(2, patientId: 2);

        var result = await _sut.GetAppointmentsAsync(TenantId, new AppointmentQueryParams
        {
            PatientId = 1, PageSize = 20, PageNumber = 1
        });

        result.Appointments.Should().ContainSingle().Which.PatientId.Should().Be(1);
    }

    // ── GetAppointmentByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAppointmentByIdAsync_ShouldReturn_WhenFound()
    {
        AddAppointment(1);

        var result = await _sut.GetAppointmentByIdAsync(TenantId, appointmentId: 1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetAppointmentByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _sut.GetAppointmentByIdAsync(TenantId, appointmentId: 999);
        result.Should().BeNull();
    }

    // ── CreateAppointmentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateAppointmentAsync_ShouldPersistAppointment()
    {
        var request = new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 10, DepartmentId = 1,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = "09:00", EndTime = "09:30",
            Type = AppointmentType.Consultation
        };

        var result = await _sut.CreateAppointmentAsync(TenantId, createdByUserId: 1, request);

        result.Status.Should().Be("Scheduled");
        _context.Appointments.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldThrow_WhenEndTimeBeforeStartTime()
    {
        var request = new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 10, DepartmentId = 1,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = "10:00", EndTime = "09:00",
            Type = AppointmentType.Consultation
        };

        await _sut.Invoking(s => s.CreateAppointmentAsync(TenantId, 1, request))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*after start*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldThrow_WhenStartTimeInvalid()
    {
        var request = new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 10, DepartmentId = 1,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = "bad_time", EndTime = "09:30",
            Type = AppointmentType.Consultation
        };

        await _sut.Invoking(s => s.CreateAppointmentAsync(TenantId, 1, request))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*start time*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldDefaultToMainBranch()
    {
        var request = new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 10, DepartmentId = 1,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = "09:00", EndTime = "09:30",
            Type = AppointmentType.Consultation
        };

        var result = await _sut.CreateAppointmentAsync(TenantId, createdByUserId: 1, request);

        result.BranchId.Should().Be(1);
        result.BranchName.Should().Be("Main Branch");
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldThrow_WhenBranchNotInTenant()
    {
        var request = new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 10, DepartmentId = 1, BranchId = 999,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = "09:00", EndTime = "09:30",
            Type = AppointmentType.Consultation
        };

        await _sut.Invoking(s => s.CreateAppointmentAsync(TenantId, 1, request))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*Branch not found*");
    }

    [Fact]
    public async Task GetAppointmentsAsync_ShouldFilterByBranchId()
    {
        AddAppointment(1);
        _context.Appointments.Add(new Appointment
        {
            Id = 2, TenantId = TenantId, BranchId = 2, PatientId = 1, DoctorId = 10, DepartmentId = 1,
            AppointmentDate = DateTime.UtcNow.Date, StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11), Status = AppointmentStatus.Scheduled,
            Type = AppointmentType.Consultation, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetAppointmentsAsync(TenantId,
            new AppointmentQueryParams { BranchId = 2, PageSize = 20, PageNumber = 1 });

        result.Appointments.Should().ContainSingle().Which.BranchId.Should().Be(2);
    }

    // ── UpdateAppointmentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateAppointmentAsync_ShouldUpdateReason()
    {
        AddAppointment(1);

        var request = new UpdateAppointmentRequest { Reason = "Follow-up checkup" };
        var result = await _sut.UpdateAppointmentAsync(TenantId, appointmentId: 1, request);

        result.Reason.Should().Be("Follow-up checkup");
    }

    [Fact]
    public async Task UpdateAppointmentAsync_ShouldThrow_WhenNotFound()
    {
        await _sut.Invoking(s => s.UpdateAppointmentAsync(TenantId, 999, new UpdateAppointmentRequest()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateStatusAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ShouldSetCheckedInAt_WhenCheckedIn()
    {
        AddAppointment(1);

        var result = await _sut.UpdateStatusAsync(TenantId, 1, AppointmentStatus.CheckedIn);

        result.Status.Should().Be("CheckedIn");
        var appt = await _context.Appointments.FindAsync(1);
        appt!.CheckedInAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldSetCompletedAt_WhenCompleted()
    {
        AddAppointment(1);

        await _sut.UpdateStatusAsync(TenantId, 1, AppointmentStatus.Completed);

        var appt = await _context.Appointments.FindAsync(1);
        appt!.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldSetCancelledAt_WhenCancelled()
    {
        AddAppointment(1);

        await _sut.UpdateStatusAsync(TenantId, 1, AppointmentStatus.Cancelled);

        var appt = await _context.Appointments.FindAsync(1);
        appt!.CancelledAt.Should().NotBeNull();
    }

    // ── CancelAppointmentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CancelAppointmentAsync_ShouldSetCancelReasonAndStatus()
    {
        AddAppointment(1);

        var result = await _sut.CancelAppointmentAsync(TenantId, 1, "Patient request");

        result.Status.Should().Be("Cancelled");
        result.CancelReason.Should().Be("Patient request");
    }

    [Fact]
    public async Task CancelAppointmentAsync_ShouldThrow_WhenNotFound()
    {
        await _sut.Invoking(s => s.CancelAppointmentAsync(TenantId, 999, "reason"))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeleteAppointmentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteAppointmentAsync_ShouldMarkCancelledWithAdminReason()
    {
        AddAppointment(1);

        await _sut.DeleteAppointmentAsync(TenantId, 1, adminUserId: 99);

        var appt = await _context.Appointments.FindAsync(1);
        appt!.Status.Should().Be(AppointmentStatus.Cancelled);
        appt.CancelReason.Should().Be("Deleted by admin");
    }

    // ── GetAvailableSlotsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldReturnSlots_WhenScheduleExists()
    {
        var date = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc); // Monday
        _context.DoctorSchedules.Add(new DoctorSchedule
        {
            Id = 1, TenantId = TenantId, DoctorId = 10,
            DayOfWeek = date.DayOfWeek,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11),
            SlotDurationMinutes = 30,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetAvailableSlotsAsync(TenantId, new AvailableSlotsQueryParams
        {
            DoctorId = 10, Date = date
        });

        result.Should().HaveCount(4);
        result[0].StartTime.Should().Be("09:00");
        result[0].EndTime.Should().Be("09:30");
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldExcludeBookedSlots()
    {
        var date = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc);
        _context.DoctorSchedules.Add(new DoctorSchedule
        {
            Id = 1, TenantId = TenantId, DoctorId = 10,
            DayOfWeek = date.DayOfWeek,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            SlotDurationMinutes = 30,
            IsActive = true
        });
        _context.Appointments.Add(new Appointment
        {
            Id = 1, TenantId = TenantId, PatientId = 1, DoctorId = 10, DepartmentId = 1,
            AppointmentDate = date.Date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)),
            Status = AppointmentStatus.Scheduled,
            Type = AppointmentType.Consultation, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetAvailableSlotsAsync(TenantId, new AvailableSlotsQueryParams
        {
            DoctorId = 10, Date = date
        });

        result.Should().ContainSingle().Which.StartTime.Should().Be("09:30");
    }

    public void Dispose() => _context.Dispose();
}
