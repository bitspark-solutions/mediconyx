using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using mediconyx_api.Data;
using mediconyx_api.Middleware;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Common;
using mediconyx_api.Services.Inpatient;

namespace mediconyx_api.Controllers;

/// <summary>
/// In-Patient Department (IPD) — wards, beds, admissions, transfers, discharge, billing.
/// All endpoints are tenant-scoped: data is filtered by TenantId from the JWT claim.
/// </summary>
[ApiController]
[Route("api/ipd")]
[Authorize]
public class IPDController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IPDController> _logger;
    private readonly IBedBillingService _billing;

    public IPDController(
        ApplicationDbContext context,
        ILogger<IPDController> logger,
        IBedBillingService billing)
    {
        _context = context;
        _logger = logger;
        _billing = billing;
    }

    private int? TenantId => ClaimsHelper.GetTenantId(User);
    private int? UserId => ClaimsHelper.GetUserId(User);

    // ═══ WARDS ═══════════════════════════════════════════════

    [HttpGet("wards")]
    [HasPermission(PermissionCatalog.ADMISSION_VIEW)]
    public async Task<ActionResult<List<WardDto>>> GetWards()
    {
        if (TenantId == null) return Unauthorized();
        var wards = await _context.Wards
            .Where(w => w.TenantId == TenantId)
            .OrderBy(w => w.Floor).ThenBy(w => w.Name)
            .Select(w => new WardDto
            {
                Id = w.Id,
                TenantId = w.TenantId,
                Name = w.Name,
                Code = w.Code,
                WardType = w.WardType,
                Location = w.Location,
                Floor = w.Floor,
                Capacity = w.Capacity,
                DefaultDailyRate = w.DefaultDailyRate,
                IsActive = w.IsActive,
                BedCount = w.Beds.Count(),
                AvailableBedCount = w.Beds.Count(b => b.Status == BedStatus.Available),
                OccupiedBedCount = w.Beds.Count(b => b.Status == BedStatus.Occupied)
            })
            .ToListAsync();
        return Ok(wards);
    }

    [HttpGet("wards/{id}")]
    [HasPermission(PermissionCatalog.ADMISSION_VIEW)]
    public async Task<ActionResult<WardDto>> GetWard(int id)
    {
        if (TenantId == null) return Unauthorized();
        var w = await _context.Wards
            .Where(x => x.Id == id && x.TenantId == TenantId)
            .Select(w => new WardDto
            {
                Id = w.Id, TenantId = w.TenantId, Name = w.Name, Code = w.Code,
                WardType = w.WardType, Location = w.Location, Floor = w.Floor,
                Capacity = w.Capacity, DefaultDailyRate = w.DefaultDailyRate,
                IsActive = w.IsActive,
                BedCount = w.Beds.Count(),
                AvailableBedCount = w.Beds.Count(b => b.Status == BedStatus.Available),
                OccupiedBedCount = w.Beds.Count(b => b.Status == BedStatus.Occupied)
            })
            .FirstOrDefaultAsync();
        if (w == null) return NotFound();
        return Ok(w);
    }

    [HttpPost("wards")]
    [HasPermission(PermissionCatalog.WARD_MANAGE)]
    public async Task<ActionResult<WardDto>> CreateWard(CreateWardRequest req)
    {
        if (TenantId == null) return Unauthorized();
        if (!WardTypes.All.Contains(req.WardType))
            return BadRequest(new { message = $"WardType must be one of: {string.Join(", ", WardTypes.All)}" });

        // Code must be unique within tenant
        if (await _context.Wards.AnyAsync(w => w.TenantId == TenantId && w.Code == req.Code))
            return BadRequest(new { message = $"Ward code '{req.Code}' already exists." });

        var ward = new Ward
        {
            TenantId = TenantId.Value,
            Name = req.Name,
            Code = req.Code,
            WardType = req.WardType,
            Location = req.Location,
            Floor = req.Floor,
            Capacity = req.Capacity,
            DefaultDailyRate = req.DefaultDailyRate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Wards.Add(ward);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetWard), new { id = ward.Id }, new WardDto
        {
            Id = ward.Id, TenantId = ward.TenantId, Name = ward.Name, Code = ward.Code,
            WardType = ward.WardType, Location = ward.Location, Floor = ward.Floor,
            Capacity = ward.Capacity, DefaultDailyRate = ward.DefaultDailyRate, IsActive = ward.IsActive
        });
    }

    [HttpPatch("wards/{id}")]
    [HasPermission(PermissionCatalog.WARD_MANAGE)]
    public async Task<ActionResult<WardDto>> UpdateWard(int id, UpdateWardRequest req)
    {
        if (TenantId == null) return Unauthorized();
        var ward = await _context.Wards.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == TenantId);
        if (ward == null) return NotFound();
        if (req.Name != null) ward.Name = req.Name;
        if (req.Location != null) ward.Location = req.Location;
        if (req.Floor.HasValue) ward.Floor = req.Floor.Value;
        if (req.Capacity.HasValue) ward.Capacity = req.Capacity.Value;
        if (req.DefaultDailyRate.HasValue) ward.DefaultDailyRate = req.DefaultDailyRate.Value;
        if (req.IsActive.HasValue) ward.IsActive = req.IsActive.Value;
        ward.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new WardDto
        {
            Id = ward.Id, TenantId = ward.TenantId, Name = ward.Name, Code = ward.Code,
            WardType = ward.WardType, Location = ward.Location, Floor = ward.Floor,
            Capacity = ward.Capacity, DefaultDailyRate = ward.DefaultDailyRate, IsActive = ward.IsActive
        });
    }

    [HttpDelete("wards/{id}")]
    [HasPermission(PermissionCatalog.WARD_MANAGE)]
    public async Task<IActionResult> DeleteWard(int id)
    {
        if (TenantId == null) return Unauthorized();
        var ward = await _context.Wards
            .Include(w => w.Beds)
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == TenantId);
        if (ward == null) return NotFound();
        if (ward.Beds.Any(b => b.Status == BedStatus.Occupied))
            return BadRequest(new { message = "Cannot delete ward with occupied beds." });
        _context.Wards.Remove(ward);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ═══ BEDS ════════════════════════════════════════════════

    [HttpGet("beds")]
    [HasPermission(PermissionCatalog.ADMISSION_VIEW)]
    public async Task<ActionResult<List<BedDto>>> GetBeds([FromQuery] int? wardId = null, [FromQuery] string? status = null)
    {
        if (TenantId == null) return Unauthorized();
        var q = _context.Beds
            .Include(b => b.Ward)
            .Where(b => b.TenantId == TenantId);
        if (wardId.HasValue) q = q.Where(b => b.WardId == wardId.Value);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BedStatus>(status, out var s))
            q = q.Where(b => b.Status == s);

        var beds = await q.OrderBy(b => b.WardId).ThenBy(b => b.Number).ToListAsync();
        var activeAdmissionByBed = await _context.Admissions
            .Where(a => a.TenantId == TenantId && a.Status == AdmissionStatus.Active)
            .ToDictionaryAsync(a => a.BedId, a => a);

        var result = beds.Select(b => new BedDto
        {
            Id = b.Id,
            TenantId = b.TenantId,
            WardId = b.WardId,
            WardName = b.Ward.Name,
            WardType = b.Ward.WardType,
            Number = b.Number,
            Kind = b.Kind.ToString(),
            CabinType = b.CabinType,
            Facilities = b.Facilities,
            Status = b.Status.ToString(),
            DailyRate = b.DailyRate ?? b.Ward.DefaultDailyRate,
            Notes = b.Notes,
            IsActive = b.IsActive,
            CurrentAdmissionId = activeAdmissionByBed.TryGetValue(b.Id, out var adm) ? adm.Id : null,
            CurrentPatientId = adm?.PatientId,
            CurrentPatientName = null, // resolved in second pass below
            CurrentPatientNumber = null
        }).ToList();
        return Ok(result);
    }

    [HttpPost("beds")]
    [HasPermission(PermissionCatalog.BED_MANAGE)]
    public async Task<ActionResult<BedDto>> CreateBed(CreateBedRequest req)
    {
        if (TenantId == null) return Unauthorized();
        var ward = await _context.Wards.FirstOrDefaultAsync(w => w.Id == req.WardId && w.TenantId == TenantId);
        if (ward == null) return BadRequest(new { message = "Ward not found in this tenant." });

        if (await _context.Beds.AnyAsync(b => b.WardId == req.WardId && b.Number == req.Number))
            return BadRequest(new { message = $"Bed '{req.Number}' already exists in this ward." });

        if (!Enum.TryParse<BedStatus>(req.Status, out var status))
            return BadRequest(new { message = $"Invalid status. Allowed: {string.Join(", ", Enum.GetNames<BedStatus>())}" });

        if (!Enum.TryParse<BedKind>(req.Kind, out var kind))
            return BadRequest(new { message = $"Invalid kind. Allowed: {string.Join(", ", Enum.GetNames<BedKind>())}" });

        string? cabinType = null;
        string? facilitiesJson = null;
        if (kind == BedKind.Cabin)
        {
            if (string.IsNullOrWhiteSpace(req.CabinType) || !CabinTypes.All.Contains(req.CabinType))
                return BadRequest(new { message = $"Invalid cabin type. Allowed: {string.Join(", ", CabinTypes.All)}" });
            cabinType = req.CabinType;
            facilitiesJson = JsonSerializer.Serialize(req.Facilities ?? CabinTypes.DefaultFacilities[cabinType]);
        }

        var bed = new Bed
        {
            TenantId = TenantId.Value,
            WardId = req.WardId,
            Number = req.Number,
            Kind = kind,
            CabinType = cabinType,
            Facilities = facilitiesJson,
            Status = status,
            DailyRate = req.DailyRate,
            Notes = req.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Beds.Add(bed);
        await _context.SaveChangesAsync();
        return Ok(new BedDto
        {
            Id = bed.Id, TenantId = bed.TenantId, WardId = bed.WardId,
            WardName = ward.Name, WardType = ward.WardType,
            Number = bed.Number, Kind = bed.Kind.ToString(),
            CabinType = bed.CabinType, Facilities = bed.Facilities,
            Status = bed.Status.ToString(),
            DailyRate = bed.DailyRate ?? ward.DefaultDailyRate,
            Notes = bed.Notes, IsActive = bed.IsActive
        });
    }

    [HttpPatch("beds/{id}/status")]
    [HasPermission(PermissionCatalog.BED_MANAGE)]
    public async Task<ActionResult<BedDto>> UpdateBedStatus(int id, UpdateBedStatusRequest req)
    {
        if (TenantId == null) return Unauthorized();
        var bed = await _context.Beds.Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == TenantId);
        if (bed == null) return NotFound();
        if (!Enum.TryParse<BedStatus>(req.Status, out var newStatus))
            return BadRequest(new { message = $"Invalid status. Allowed: {string.Join(", ", Enum.GetNames<BedStatus>())}" });

        // Can't mark Occupied via this endpoint -- it's set by admission
        if (newStatus == BedStatus.Occupied)
            return BadRequest(new { message = "Bed becomes Occupied automatically when a patient is admitted. Use /admissions to discharge first." });

        bed.Status = newStatus;
        bed.Notes = req.Notes ?? bed.Notes;
        if (req.DailyRate.HasValue) bed.DailyRate = req.DailyRate.Value;
        if (bed.Kind == BedKind.Cabin)
        {
            if (req.CabinType != null)
            {
                if (!CabinTypes.All.Contains(req.CabinType))
                    return BadRequest(new { message = $"Invalid cabin type. Allowed: {string.Join(", ", CabinTypes.All)}" });
                bed.CabinType = req.CabinType;
            }
            if (req.Facilities != null)
                bed.Facilities = JsonSerializer.Serialize(req.Facilities);
        }
        bed.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new BedDto
        {
            Id = bed.Id, TenantId = bed.TenantId, WardId = bed.WardId,
            WardName = bed.Ward.Name, WardType = bed.Ward.WardType,
            Number = bed.Number, Kind = bed.Kind.ToString(),
            CabinType = bed.CabinType, Facilities = bed.Facilities,
            Status = bed.Status.ToString(),
            DailyRate = bed.DailyRate ?? bed.Ward.DefaultDailyRate,
            Notes = bed.Notes, IsActive = bed.IsActive
        });
    }

    [HttpDelete("beds/{id}")]
    [HasPermission(PermissionCatalog.BED_MANAGE)]
    public async Task<IActionResult> DeleteBed(int id)
    {
        if (TenantId == null) return Unauthorized();
        var bed = await _context.Beds.FirstOrDefaultAsync(b => b.Id == id && b.TenantId == TenantId);
        if (bed == null) return NotFound();
        if (bed.Status == BedStatus.Occupied)
            return BadRequest(new { message = "Cannot delete an occupied bed. Discharge the patient first." });
        _context.Beds.Remove(bed);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ═══ CABINS ═══════════════════════════════════════════════

    /// <summary>List cabin units (beds with Kind = Cabin), optionally filtered.</summary>
    [HttpGet("cabins")]
    [HasPermission(PermissionCatalog.ADMISSION_VIEW)]
    public async Task<ActionResult<List<BedDto>>> GetCabins([FromQuery] string? status = null, [FromQuery] string? cabinType = null)
    {
        if (TenantId == null) return Unauthorized();
        var q = _context.Beds
            .Include(b => b.Ward)
            .Where(b => b.TenantId == TenantId && b.Kind == BedKind.Cabin);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BedStatus>(status, out var s))
            q = q.Where(b => b.Status == s);
        if (!string.IsNullOrEmpty(cabinType))
            q = q.Where(b => b.CabinType == cabinType);

        var cabins = await q.OrderBy(b => b.WardId).ThenBy(b => b.Number).ToListAsync();
        var activeAdmissionByBed = await _context.Admissions
            .Where(a => a.TenantId == TenantId && a.Status == AdmissionStatus.Active)
            .ToDictionaryAsync(a => a.BedId, a => a);

        var result = cabins.Select(b => new BedDto
        {
            Id = b.Id,
            TenantId = b.TenantId,
            WardId = b.WardId,
            WardName = b.Ward.Name,
            WardType = b.Ward.WardType,
            Number = b.Number,
            Kind = b.Kind.ToString(),
            CabinType = b.CabinType,
            Facilities = b.Facilities,
            Status = b.Status.ToString(),
            DailyRate = b.DailyRate ?? b.Ward.DefaultDailyRate,
            Notes = b.Notes,
            IsActive = b.IsActive,
            CurrentAdmissionId = activeAdmissionByBed.TryGetValue(b.Id, out var adm) ? adm.Id : null,
            CurrentPatientId = adm?.PatientId
        }).ToList();
        return Ok(result);
    }

    /// <summary>Static catalog of cabin categories with default amenities and typical BDT rates.</summary>
    [HttpGet("cabin-types")]
    [HasPermission(PermissionCatalog.ADMISSION_VIEW)]
    public ActionResult<List<CabinTypeInfoDto>> GetCabinTypes()
    {
        var result = CabinTypes.All.Select(t => new CabinTypeInfoDto
        {
            Type = t,
            DefaultFacilities = CabinTypes.DefaultFacilities[t],
            TypicalRateMinBdt = CabinTypes.TypicalRatesBdt[t].Min,
            TypicalRateMaxBdt = CabinTypes.TypicalRatesBdt[t].Max
        }).ToList();
        return Ok(result);
    }

    // ═══ ADMISSIONS ═════════════════════════════════════════

    [HttpGet("admissions")]
    [HasPermission(PermissionCatalog.ADMISSION_VIEW)]
    public async Task<ActionResult<List<AdmissionDto>>> GetAdmissions(
        [FromQuery] int? patientId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool activeOnly = false)
    {
        if (TenantId == null) return Unauthorized();
        var q = _context.Admissions
            .Include(a => a.Patient)
            .Include(a => a.Bed).ThenInclude(b => b.Ward)
            .Include(a => a.AttendingDoctor).ThenInclude(d => d.User)
            .Include(a => a.Transfers).ThenInclude(t => t.FromBed)
            .Include(a => a.Transfers).ThenInclude(t => t.ToBed)
            .Include(a => a.DischargeSummary).ThenInclude(s => s!.DischargingDoctor).ThenInclude(d => d.User)
            .Where(a => a.TenantId == TenantId);
        if (patientId.HasValue) q = q.Where(a => a.PatientId == patientId.Value);
        if (activeOnly) q = q.Where(a => a.Status == AdmissionStatus.Active);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AdmissionStatus>(status, out var s))
            q = q.Where(a => a.Status == s);

        var list = await q.OrderByDescending(a => a.AdmissionDate).ToListAsync();
        return Ok(list.Select(MapAdmission).ToList());
    }

    [HttpGet("admissions/{id}")]
    [HasPermission(PermissionCatalog.ADMISSION_VIEW)]
    public async Task<ActionResult<AdmissionDto>> GetAdmission(int id)
    {
        if (TenantId == null) return Unauthorized();
        var a = await _context.Admissions
            .Include(x => x.Patient)
            .Include(x => x.Bed).ThenInclude(b => b.Ward)
            .Include(x => x.AttendingDoctor).ThenInclude(d => d.User)
            .Include(x => x.Transfers).ThenInclude(t => t.FromBed)
            .Include(x => x.Transfers).ThenInclude(t => t.ToBed)
            .Include(x => x.DischargeSummary).ThenInclude(s => s!.DischargingDoctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
        if (a == null) return NotFound();
        return Ok(MapAdmission(a));
    }

    [HttpPost("admissions")]
    [HasPermission(PermissionCatalog.ADMISSION_CREATE)]
    public async Task<ActionResult<AdmissionDto>> CreateAdmission(CreateAdmissionRequest req)
    {
        if (TenantId == null) return Unauthorized();
        if (UserId == null) return Unauthorized();

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == req.PatientId && p.TenantId == TenantId);
        if (patient == null) return BadRequest(new { message = "Patient not found in this tenant." });

        var bed = await _context.Beds.Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.Id == req.BedId && b.TenantId == TenantId);
        if (bed == null) return BadRequest(new { message = "Bed not found in this tenant." });
        if (bed.Status != BedStatus.Available)
            return BadRequest(new { message = $"Bed '{bed.Number}' is {bed.Status}, not Available." });

        var doctor = await _context.DoctorProfiles.Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == req.AttendingDoctorId && d.TenantId == TenantId);
        if (doctor == null) return BadRequest(new { message = "Attending doctor not found in this tenant." });

        if (!Enum.TryParse<AdmissionType>(req.AdmissionType, out var admissionType))
            return BadRequest(new { message = $"AdmissionType must be one of: {string.Join(", ", Enum.GetNames<AdmissionType>())}" });

        var admission = new Admission
        {
            TenantId = TenantId.Value,
            PatientId = req.PatientId,
            BedId = req.BedId,
            AttendingDoctorId = req.AttendingDoctorId,
            AdmissionType = admissionType,
            AdmissionDate = DateTime.UtcNow,
            ExpectedDischargeDate = req.ExpectedDischargeDate,
            Reason = req.Reason,
            DiagnosisOnAdmission = req.DiagnosisOnAdmission,
            Status = AdmissionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        bed.Status = BedStatus.Occupied;
        bed.UpdatedAt = DateTime.UtcNow;
        _context.Admissions.Add(admission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Patient {PatientId} admitted to bed {BedId} in tenant {TenantId}", req.PatientId, req.BedId, TenantId);

        // Refetch with includes
        var created = await _context.Admissions
            .Include(x => x.Patient)
            .Include(x => x.Bed).ThenInclude(b => b.Ward)
            .Include(x => x.AttendingDoctor).ThenInclude(d => d.User)
            .FirstAsync(x => x.Id == admission.Id);
        return CreatedAtAction(nameof(GetAdmission), new { id = admission.Id }, MapAdmission(created));
    }

    [HttpPost("admissions/{id}/transfer")]
    [HasPermission(PermissionCatalog.ADMISSION_TRANSFER)]
    public async Task<ActionResult<AdmissionDto>> TransferAdmission(int id, TransferAdmissionRequest req)
    {
        if (TenantId == null) return Unauthorized();
        if (UserId == null) return Unauthorized();
        var admission = await _context.Admissions
            .Include(a => a.Bed)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == TenantId);
        if (admission == null) return NotFound();
        if (admission.Status != AdmissionStatus.Active)
            return BadRequest(new { message = "Only active admissions can be transferred." });

        var newBed = await _context.Beds.Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.Id == req.ToBedId && b.TenantId == TenantId);
        if (newBed == null) return BadRequest(new { message = "Destination bed not found." });
        if (newBed.Id == admission.BedId) return BadRequest(new { message = "Patient is already in this bed." });
        if (newBed.Status != BedStatus.Available)
            return BadRequest(new { message = $"Destination bed '{newBed.Number}' is {newBed.Status}, not Available." });

        var fromBed = admission.Bed;
        var transfer = new AdmissionTransfer
        {
            AdmissionId = admission.Id,
            FromBedId = fromBed.Id,
            ToBedId = newBed.Id,
            TransferredAt = DateTime.UtcNow,
            TransferredByUserId = UserId.Value,
            Reason = req.Reason
        };
        _context.AdmissionTransfers.Add(transfer);

        fromBed.Status = BedStatus.Cleaning;
        newBed.Status = BedStatus.Occupied;
        admission.BedId = newBed.Id;
        admission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admission {AdmissionId} transferred from bed {From} to {To}", id, fromBed.Number, newBed.Number);

        var refreshed = await _context.Admissions
            .Include(x => x.Patient)
            .Include(x => x.Bed).ThenInclude(b => b.Ward)
            .Include(x => x.AttendingDoctor).ThenInclude(d => d.User)
            .Include(x => x.Transfers).ThenInclude(t => t.FromBed)
            .Include(x => x.Transfers).ThenInclude(t => t.ToBed)
            .FirstAsync(x => x.Id == id);
        return Ok(MapAdmission(refreshed));
    }

    [HttpPost("admissions/{id}/discharge")]
    [HasPermission(PermissionCatalog.ADMISSION_DISCHARGE)]
    public async Task<ActionResult<AdmissionDto>> DischargeAdmission(int id, DischargeAdmissionRequest req)
    {
        if (TenantId == null) return Unauthorized();
        var admission = await _context.Admissions
            .Include(a => a.Bed)
            .Include(a => a.DischargeSummary)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == TenantId);
        if (admission == null) return NotFound();
        if (admission.Status != AdmissionStatus.Active)
            return BadRequest(new { message = "Only active admissions can be discharged." });

        var doctor = await _context.DoctorProfiles.Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == req.DischargingDoctorId && d.TenantId == TenantId);
        if (doctor == null) return BadRequest(new { message = "Discharging doctor not found." });

        if (!Enum.TryParse<DischargeCondition>(req.ConditionAtDischarge, out var condition))
            return BadRequest(new { message = $"ConditionAtDischarge must be one of: {string.Join(", ", Enum.GetNames<DischargeCondition>())}" });

        var now = DateTime.UtcNow;
        admission.Status = condition == DischargeCondition.Deceased
            ? AdmissionStatus.Deceased
            : condition == DischargeCondition.LAMA
                ? AdmissionStatus.LAMA
                : AdmissionStatus.Discharged;
        admission.DischargeDate = now;
        admission.DischargeNotes = req.DischargeInstructions;
        admission.UpdatedAt = now;

        // Free the bed
        admission.Bed.Status = BedStatus.Cleaning;
        admission.Bed.UpdatedAt = now;

        // Create discharge summary
        if (admission.DischargeSummary == null)
        {
            _context.DischargeSummaries.Add(new DischargeSummary
            {
                AdmissionId = admission.Id,
                DischargingDoctorId = req.DischargingDoctorId,
                DischargeDate = now,
                ConditionAtDischarge = condition,
                DischargeInstructions = req.DischargeInstructions,
                FollowUpDate = req.FollowUpDate,
                MedicationsOnDischarge = req.MedicationsOnDischarge,
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admission {AdmissionId} discharged ({Condition})", id, condition);

        var refreshed = await _context.Admissions
            .Include(x => x.Patient)
            .Include(x => x.Bed).ThenInclude(b => b.Ward)
            .Include(x => x.AttendingDoctor).ThenInclude(d => d.User)
            .Include(x => x.Transfers).ThenInclude(t => t.FromBed)
            .Include(x => x.Transfers).ThenInclude(t => t.ToBed)
            .Include(x => x.DischargeSummary).ThenInclude(s => s!.DischargingDoctor).ThenInclude(d => d.User)
            .FirstAsync(x => x.Id == id);
        return Ok(MapAdmission(refreshed));
    }

    // ═══ BED BOARD ══════════════════════════════════════════

    [HttpGet("bed-board")]
    [HasPermission(PermissionCatalog.ADMISSION_VIEW)]
    public async Task<ActionResult<List<BedBoardWardDto>>> GetBedBoard()
    {
        if (TenantId == null) return Unauthorized();
        var wards = await _context.Wards
            .Where(w => w.TenantId == TenantId && w.IsActive)
            .OrderBy(w => w.Floor).ThenBy(w => w.Name)
            .Include(w => w.Beds)
            .ToListAsync();

        var activeAdmByBed = await _context.Admissions
            .Include(a => a.Patient)
            .Include(a => a.AttendingDoctor).ThenInclude(d => d.User)
            .Where(a => a.TenantId == TenantId && a.Status == AdmissionStatus.Active)
            .ToDictionaryAsync(a => a.BedId);

        var board = wards.Select(w => new BedBoardWardDto
        {
            Id = w.Id,
            Name = w.Name,
            Code = w.Code,
            WardType = w.WardType,
            Floor = w.Floor,
            Beds = w.Beds.OrderBy(b => b.Number).Select(b =>
            {
                activeAdmByBed.TryGetValue(b.Id, out var adm);
                return new BedBoardBedDto
                {
                    Id = b.Id,
                    Number = b.Number,
                    Kind = b.Kind.ToString(),
                    CabinType = b.CabinType,
                    Facilities = b.Facilities,
                    Status = b.Status.ToString(),
                    IsActive = b.IsActive,
                    AdmissionId = adm?.Id,
                    PatientId = adm?.PatientId,
                    PatientName = adm != null ? $"{adm.Patient.FirstName} {adm.Patient.LastName}" : null,
                    PatientNumber = adm?.Patient.PatientNumber,
                    AttendingDoctorName = adm != null ? $"Dr. {adm.AttendingDoctor.User.LastName}" : null,
                    AdmissionDate = adm?.AdmissionDate
                };
            }).ToList()
        }).ToList();
        return Ok(board);
    }

    // ═══ BILLING (Tier 3.1) ════════════════════════════════

    /// <summary>
    /// Generate (or refresh) the per-bed-day bill for an admission. Idempotent: re-running
    /// after a charge is already recorded for a given day will NOT double-bill. The bill
    /// window runs from admission date to discharge date (or today if still active).
    /// </summary>
    [HttpPost("admissions/{id}/generate-bill")]
    [HasPermission(PermissionCatalog.INVOICE_CREATE)]
    public async Task<ActionResult<BillingResult>> GenerateBill(int id)
    {
        if (TenantId == null) return Unauthorized();
        if (UserId == null) return Unauthorized();

        try
        {
            var result = await _billing.GenerateForAdmissionAsync(id, TenantId.Value, UserId.Value);
            _logger.LogInformation("Generated {Count} new bed charges for admission {AdmissionId}; total {Total} BDT",
                result.NewChargesCreated, id, result.TotalAmount);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>
    /// View the full per-day bill for an admission, including all generated charges and the total.
    /// </summary>
    [HttpGet("admissions/{id}/bill")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<AdmissionBillDto>> GetBill(int id)
    {
        if (TenantId == null) return Unauthorized();
        try { return Ok(await _billing.GetBillAsync(id, TenantId.Value)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>
    /// Convenience: a per-day calendar of every charged bed for the current tenant.
    /// Useful for nightly reconciliation reports. Defaults to today.
    /// </summary>
    [HttpGet("billing/charges")]
    [HasPermission(PermissionCatalog.INVOICE_VIEW)]
    public async Task<ActionResult<List<BedChargeDto>>> GetChargesByDate([FromQuery] DateTime? date = null)
    {
        if (TenantId == null) return Unauthorized();
        // Query-string dates come in as Kind=Unspecified, which Npgsql rejects for
        // "timestamp with time zone". Normalize to UTC before comparing.
        var day = (date ?? DateTime.UtcNow).Date;
        var dayUtc = DateTime.SpecifyKind(day, DateTimeKind.Utc);
        var charges = await _context.BedCharges
            .Include(c => c.Bed).ThenInclude(b => b.Ward)
            .Where(c => c.TenantId == TenantId && c.ChargeDate == dayUtc)
            .OrderBy(c => c.Bed!.Ward!.Name).ThenBy(c => c.Bed!.Number)
            .Select(c => new BedChargeDto
            {
                Id = c.Id,
                ChargeDate = c.ChargeDate,
                BedId = c.BedId,
                BedNumber = c.Bed!.Number,
                WardName = c.Bed!.Ward!.Name,
                WardType = c.Bed!.Ward!.WardType,
                DailyRate = c.DailyRate,
                Amount = c.Amount,
                Description = c.Description
            })
            .ToListAsync();
        return Ok(charges);
    }

    // ═══ Private helpers ═══════════════════════════════════

    private static AdmissionDto MapAdmission(Admission a) => new()
    {
        Id = a.Id,
        TenantId = a.TenantId,
        PatientId = a.PatientId,
        PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
        PatientNumber = a.Patient.PatientNumber,
        BedId = a.BedId,
        BedNumber = a.Bed.Number,
        WardId = a.Bed.WardId,
        WardName = a.Bed.Ward.Name,
        WardType = a.Bed.Ward.WardType,
        AdmissionType = a.AdmissionType.ToString(),
        AdmissionDate = a.AdmissionDate,
        ExpectedDischargeDate = a.ExpectedDischargeDate,
        AttendingDoctorId = a.AttendingDoctorId,
        AttendingDoctorName = $"Dr. {a.AttendingDoctor.User.LastName}",
        Reason = a.Reason,
        DiagnosisOnAdmission = a.DiagnosisOnAdmission,
        Status = a.Status.ToString(),
        DischargeDate = a.DischargeDate,
        DischargeNotes = a.DischargeNotes,
        Transfers = a.Transfers.OrderBy(t => t.TransferredAt).Select(t => new AdmissionTransferDto
        {
            Id = t.Id,
            FromBedId = t.FromBedId,
            FromBedNumber = t.FromBed.Number,
            ToBedId = t.ToBedId,
            ToBedNumber = t.ToBed.Number,
            TransferredAt = t.TransferredAt,
            TransferredByUserId = t.TransferredByUserId,
            Reason = t.Reason
        }).ToList(),
        DischargeSummary = a.DischargeSummary == null ? null : new DischargeSummaryDto
        {
            Id = a.DischargeSummary.Id,
            AdmissionId = a.DischargeSummary.AdmissionId,
            DischargingDoctorId = a.DischargeSummary.DischargingDoctorId,
            DischargingDoctorName = $"Dr. {a.DischargeSummary.DischargingDoctor.User.LastName}",
            DischargeDate = a.DischargeSummary.DischargeDate,
            ConditionAtDischarge = a.DischargeSummary.ConditionAtDischarge.ToString(),
            DischargeInstructions = a.DischargeSummary.DischargeInstructions,
            FollowUpDate = a.DischargeSummary.FollowUpDate,
            MedicationsOnDischarge = a.DischargeSummary.MedicationsOnDischarge
        }
    };
}
