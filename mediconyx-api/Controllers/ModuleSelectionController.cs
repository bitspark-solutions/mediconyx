using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/sales/requests/{requestId:int}/modules")]
[Authorize(Roles = "Sales,Admin,Master")]
public class ModuleSelectionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleSelectionController> _logger;

    public ModuleSelectionController(
        ApplicationDbContext context,
        ILogger<ModuleSelectionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ModuleSelectionResponseDto>> Get(int requestId)
    {
        var modules = await _context.ModuleSelections
            .Where(m => m.SalesRequestId == requestId)
            .FirstOrDefaultAsync();

        if (modules == null)
        {
            return NotFound(new { message = "Module selection not found for this request." });
        }

        return Ok(MapToResponseDto(modules));
    }

    [HttpPost]
    public async Task<ActionResult<ModuleSelectionResponseDto>> CreateOrUpdate(
        int requestId,
        [FromBody] ModuleSelectionDto dto)
    {
        // Validate SalesRequest exists
        var salesRequest = await _context.SalesRequests
            .Include(r => r.ModuleSelection)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (salesRequest == null)
        {
            return NotFound(new { message = "Sales request not found." });
        }

        ModuleSelection modules;
        bool isNew = false;

        if (salesRequest.ModuleSelection == null)
        {
            // Create new
            modules = new ModuleSelection
            {
                SalesRequestId = requestId
            };
            isNew = true;

            // Apply default module sets based on subscription plan
            if (!string.IsNullOrEmpty(salesRequest.SubscriptionPlan))
            {
                ApplyDefaultModules(modules, salesRequest.SubscriptionPlan, dto);
            }
            else
            {
                MapDtoToEntity(dto, modules);
            }
        }
        else
        {
            // Update existing
            modules = salesRequest.ModuleSelection;
            modules.UpdatedAt = DateTime.UtcNow;
            MapDtoToEntity(dto, modules);
        }

        if (isNew)
        {
            _context.ModuleSelections.Add(modules);
        }

        // Create timeline event
        var enabledModulesCount = CountEnabledModules(modules);
        var timelineEvent = new OnboardingTimelineEvent
        {
            SalesRequestId = requestId,
            Type = "ModulesConfigured",
            Description = isNew 
                ? $"Module selection created with {enabledModulesCount} modules enabled"
                : $"Module selection updated with {enabledModulesCount} modules enabled",
            HappenedAt = DateTime.UtcNow
        };
        _context.OnboardingTimelineEvents.Add(timelineEvent);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Module selection {Action} for SalesRequest {RequestId} with {Count} modules enabled",
            isNew ? "created" : "updated",
            requestId,
            enabledModulesCount);

        var response = MapToResponseDto(modules);
        
        return isNew 
            ? CreatedAtAction(nameof(Get), new { requestId }, response)
            : Ok(response);
    }

    private void ApplyDefaultModules(ModuleSelection entity, string plan, ModuleSelectionDto dto)
    {
        // First apply user selection
        MapDtoToEntity(dto, entity);

        // Then ensure plan defaults are included
        var planLower = plan.ToLower();
        
        if (planLower.Contains("basic"))
        {
            // Basic plan modules
            entity.OpdManagement = true;
            entity.PharmacyManagement = true;
            entity.BillingInvoicing = true;
            entity.AppointmentScheduling = true;
        }
        else if (planLower.Contains("standard"))
        {
            // Standard includes Basic + additional
            entity.OpdManagement = true;
            entity.PharmacyManagement = true;
            entity.BillingInvoicing = true;
            entity.AppointmentScheduling = true;
            entity.IpdManagement = true;
            entity.Laboratory = true;
            entity.InventoryManagement = true;
        }
        else if (planLower.Contains("premium"))
        {
            // Premium includes Standard + additional
            entity.OpdManagement = true;
            entity.PharmacyManagement = true;
            entity.BillingInvoicing = true;
            entity.AppointmentScheduling = true;
            entity.IpdManagement = true;
            entity.Laboratory = true;
            entity.InventoryManagement = true;
            entity.EmergencyServices = true;
            entity.RadiologyImaging = true;
            entity.HrPayroll = true;
            entity.AnalyticsReporting = true;
        }
        else if (planLower.Contains("enterprise"))
        {
            // Enterprise: all modules
            entity.OpdManagement = true;
            entity.IpdManagement = true;
            entity.EmergencyServices = true;
            entity.PharmacyManagement = true;
            entity.Laboratory = true;
            entity.RadiologyImaging = true;
            entity.BillingInvoicing = true;
            entity.InventoryManagement = true;
            entity.HrPayroll = true;
            entity.AppointmentScheduling = true;
            entity.Telemedicine = true;
            entity.AnalyticsReporting = true;
        }
    }

    private void MapDtoToEntity(ModuleSelectionDto dto, ModuleSelection entity)
    {
        entity.OpdManagement = dto.OpdManagement;
        entity.IpdManagement = dto.IpdManagement;
        entity.EmergencyServices = dto.EmergencyServices;
        entity.PharmacyManagement = dto.PharmacyManagement;
        entity.Laboratory = dto.Laboratory;
        entity.RadiologyImaging = dto.RadiologyImaging;
        entity.BillingInvoicing = dto.BillingInvoicing;
        entity.InventoryManagement = dto.InventoryManagement;
        entity.HrPayroll = dto.HrPayroll;
        entity.AppointmentScheduling = dto.AppointmentScheduling;
        entity.Telemedicine = dto.Telemedicine;
        entity.AnalyticsReporting = dto.AnalyticsReporting;
    }

    private ModuleSelectionResponseDto MapToResponseDto(ModuleSelection entity)
    {
        return new ModuleSelectionResponseDto
        {
            Id = entity.Id,
            SalesRequestId = entity.SalesRequestId,
            OpdManagement = entity.OpdManagement,
            IpdManagement = entity.IpdManagement,
            EmergencyServices = entity.EmergencyServices,
            PharmacyManagement = entity.PharmacyManagement,
            Laboratory = entity.Laboratory,
            RadiologyImaging = entity.RadiologyImaging,
            BillingInvoicing = entity.BillingInvoicing,
            InventoryManagement = entity.InventoryManagement,
            HrPayroll = entity.HrPayroll,
            AppointmentScheduling = entity.AppointmentScheduling,
            Telemedicine = entity.Telemedicine,
            AnalyticsReporting = entity.AnalyticsReporting,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private int CountEnabledModules(ModuleSelection modules)
    {
        var count = 0;
        if (modules.OpdManagement) count++;
        if (modules.IpdManagement) count++;
        if (modules.EmergencyServices) count++;
        if (modules.PharmacyManagement) count++;
        if (modules.Laboratory) count++;
        if (modules.RadiologyImaging) count++;
        if (modules.BillingInvoicing) count++;
        if (modules.InventoryManagement) count++;
        if (modules.HrPayroll) count++;
        if (modules.AppointmentScheduling) count++;
        if (modules.Telemedicine) count++;
        if (modules.AnalyticsReporting) count++;
        return count;
    }
}
