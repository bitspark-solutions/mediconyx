using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Appointments;
using mediconyx_api.Services.Common;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    public async Task<ActionResult<AppointmentListResponse>> GetAppointments([FromQuery] AppointmentQueryParams queryParams)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        return Ok(await _appointmentService.GetAppointmentsAsync(tenantId.Value, queryParams));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        var appointment = await _appointmentService.GetAppointmentByIdAsync(tenantId.Value, id);
        return appointment == null ? NotFound(new { message = "Appointment not found." }) : Ok(appointment);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(CreateAppointmentRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        var userId = ClaimsHelper.GetUserId(User);
        if (tenantId == null || userId == null) return Unauthorized();

        if (request.PatientId <= 0) return BadRequest(new { message = "Patient ID is required." });
        if (request.DoctorId <= 0) return BadRequest(new { message = "Doctor ID is required." });
        if (request.DepartmentId <= 0) return BadRequest(new { message = "Department ID is required." });

        try
        {
            var result = await _appointmentService.CreateAppointmentAsync(tenantId.Value, userId.Value, request);
            return CreatedAtAction(nameof(GetAppointment), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, UpdateAppointmentRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        try
        {
            return Ok(await _appointmentService.UpdateAppointmentAsync(tenantId.Value, id, request));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "Appointment not found." }); }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointmentStatus(int id, [FromBody] AppointmentStatus status)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        try
        {
            return Ok(await _appointmentService.UpdateStatusAsync(tenantId.Value, id, status));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "Appointment not found." }); }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<ActionResult<AppointmentDto>> CancelAppointment(int id, CancelAppointmentRequest request)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        try
        {
            return Ok(await _appointmentService.CancelAppointmentAsync(tenantId.Value, id, request.Reason));
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "Appointment not found." }); }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        var userId = ClaimsHelper.GetUserId(User);
        if (tenantId == null || userId == null) return Unauthorized();

        try
        {
            await _appointmentService.DeleteAppointmentAsync(tenantId.Value, id, userId.Value);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(new { message = "Appointment not found." }); }
    }

    [HttpGet("slots")]
    public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots([FromQuery] AvailableSlotsQueryParams queryParams)
    {
        var tenantId = ClaimsHelper.GetTenantId(User);
        if (tenantId == null) return Unauthorized();

        return Ok(await _appointmentService.GetAvailableSlotsAsync(tenantId.Value, queryParams));
    }
}