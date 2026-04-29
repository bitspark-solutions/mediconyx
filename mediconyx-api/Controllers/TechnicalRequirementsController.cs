using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/sales/requests/{requestId:int}/technical-requirements")]
[Authorize(Roles = "Sales,Admin,Master")]
public class TechnicalRequirementsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TechnicalRequirementsController> _logger;

    public TechnicalRequirementsController(
        ApplicationDbContext context,
        ILogger<TechnicalRequirementsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<TechnicalRequirementsResponseDto>> Get(int requestId)
    {
        var techReq = await _context.TechnicalRequirements
            .Where(t => t.SalesRequestId == requestId)
            .FirstOrDefaultAsync();

        if (techReq == null)
        {
            return NotFound(new { message = "Technical requirements not found for this request." });
        }

        return Ok(MapToResponseDto(techReq));
    }

    [HttpPost]
    public async Task<ActionResult<TechnicalRequirementsResponseDto>> CreateOrUpdate(
        int requestId,
        [FromBody] TechnicalRequirementsCreateDto dto)
    {
        // Business logic validation
        if (dto.ConcurrentUsers > dto.ExpectedUserCount)
        {
            return BadRequest(new 
            { 
                message = "Concurrent users cannot exceed expected user count." 
            });
        }

        if (dto.DataMigrationRequired && !dto.EstimatedPatientRecords.HasValue)
        {
            return BadRequest(new 
            { 
                message = "Estimated patient records is required when data migration is needed." 
            });
        }

        // Validate department count matches list (rough validation)
        var departmentCount = dto.DepartmentsList
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Length;
        
        if (departmentCount < dto.DepartmentCount - 5 || departmentCount > dto.DepartmentCount + 5)
        {
            _logger.LogWarning(
                "Department count mismatch: {Declared} declared but {Actual} listed",
                dto.DepartmentCount,
                departmentCount);
        }

        // Validate SalesRequest exists
        var salesRequest = await _context.SalesRequests
            .Include(r => r.TechnicalRequirements)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (salesRequest == null)
        {
            return NotFound(new { message = "Sales request not found." });
        }

        TechnicalRequirements techReq;
        bool isNew = false;

        if (salesRequest.TechnicalRequirements == null)
        {
            // Create new
            techReq = new TechnicalRequirements
            {
                SalesRequestId = requestId
            };
            isNew = true;
        }
        else
        {
            // Update existing
            techReq = salesRequest.TechnicalRequirements;
            techReq.UpdatedAt = DateTime.UtcNow;
        }

        // Map DTO to entity
        MapDtoToEntity(dto, techReq);

        if (isNew)
        {
            _context.TechnicalRequirements.Add(techReq);
        }

        // Create timeline event
        var timelineEvent = new OnboardingTimelineEvent
        {
            SalesRequestId = requestId,
            Type = "TechnicalRequirementsSubmitted",
            Description = isNew 
                ? "Technical requirements created" 
                : "Technical requirements updated",
            HappenedAt = DateTime.UtcNow
        };
        _context.OnboardingTimelineEvents.Add(timelineEvent);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Technical requirements {Action} for SalesRequest {RequestId}",
            isNew ? "created" : "updated",
            requestId);

        var response = MapToResponseDto(techReq);
        
        return isNew 
            ? CreatedAtAction(nameof(Get), new { requestId }, response)
            : Ok(response);
    }

    private void MapDtoToEntity(TechnicalRequirementsCreateDto dto, TechnicalRequirements entity)
    {
        entity.ExpectedUserCount = dto.ExpectedUserCount;
        entity.ConcurrentUsers = dto.ConcurrentUsers;
        entity.DepartmentCount = dto.DepartmentCount;
        entity.DepartmentsList = dto.DepartmentsList;
        entity.ExistingEmrSystem = dto.ExistingEmrSystem;
        entity.IntegrationRequirements = dto.IntegrationRequirements;
        entity.DataMigrationRequired = dto.DataMigrationRequired;
        entity.EstimatedPatientRecords = dto.EstimatedPatientRecords;
        entity.PreferredGoLiveDate = dto.PreferredGoLiveDate;
        entity.ItContactName = dto.ItContactName;
        entity.ItContactEmail = dto.ItContactEmail;
        entity.ItContactPhone = dto.ItContactPhone;
    }

    private TechnicalRequirementsResponseDto MapToResponseDto(TechnicalRequirements entity)
    {
        return new TechnicalRequirementsResponseDto
        {
            Id = entity.Id,
            SalesRequestId = entity.SalesRequestId,
            ExpectedUserCount = entity.ExpectedUserCount,
            ConcurrentUsers = entity.ConcurrentUsers,
            DepartmentCount = entity.DepartmentCount,
            DepartmentsList = entity.DepartmentsList,
            ExistingEmrSystem = entity.ExistingEmrSystem,
            IntegrationRequirements = entity.IntegrationRequirements,
            DataMigrationRequired = entity.DataMigrationRequired,
            EstimatedPatientRecords = entity.EstimatedPatientRecords,
            PreferredGoLiveDate = entity.PreferredGoLiveDate,
            ItContactName = entity.ItContactName,
            ItContactEmail = entity.ItContactEmail,
            ItContactPhone = entity.ItContactPhone,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
