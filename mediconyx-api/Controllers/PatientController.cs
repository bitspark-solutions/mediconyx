using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Middleware;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Common;
using mediconyx_api.Services.Patients;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientController> _logger;

    public PatientController(IPatientService patientService, ILogger<PatientController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.PATIENT_VIEW)]
    public async Task<ActionResult<PatientListResponse>> GetPatients([FromQuery] PatientQueryParams queryParams)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        var result = await _patientService.GetPatientsAsync(tenantId.Value, queryParams);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [HasPermission(PermissionCatalog.PATIENT_VIEW)]
    public async Task<ActionResult<PatientDto>> GetPatient(int id)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        var patient = await _patientService.GetPatientByIdAsync(tenantId.Value, id);
        return patient == null ? NotFound(new { message = "Patient not found." }) : Ok(patient);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.PATIENT_CREATE)]
    public async Task<ActionResult<PatientDto>> CreatePatient(CreatePatientRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        var userId = ClaimsHelper.GetUserId(User);
        if (tenantId == null || userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return BadRequest(new { message = "First name is required." });
        if (string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "Last name is required." });
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Phone number is required." });

        var patient = await _patientService.CreatePatientAsync(tenantId.Value, userId.Value, request);
        return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
    }

    [HttpPut("{id}")]
    [HasPermission(PermissionCatalog.PATIENT_EDIT)]
    public async Task<ActionResult<PatientDto>> UpdatePatient(int id, UpdatePatientRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        try
        {
            var patient = await _patientService.UpdatePatientAsync(tenantId.Value, id, request);
            return Ok(patient);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Patient not found." });
        }
    }

    [HttpDelete("{id}")]
    [HasPermission(PermissionCatalog.PATIENT_EDIT)]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        try
        {
            await _patientService.DeactivatePatientAsync(tenantId.Value, id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Patient not found." });
        }
    }
}
