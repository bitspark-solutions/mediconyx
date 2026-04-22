using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Interfaces;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RagController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRagQueryService _ragQueryService;
    private readonly ILogger<RagController> _logger;

    public RagController(
        ApplicationDbContext context,
        IRagQueryService ragQueryService,
        ILogger<RagController> logger)
    {
        _context = context;
        _ragQueryService = ragQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Chat with AI about a patient's health data
    /// </summary>
    [HttpPost("patient/{patientId}/chat")]
    [Authorize(Roles = "Patient,Doctor,Nurse,Admin")]
    public async Task<ActionResult<RagQueryResponse>> Chat(
        int patientId,
        [FromBody] RagQueryRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (tenantId == null || userId == null)
            return Unauthorized();

        // Validate patient belongs to tenant
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId);
        if (patient == null)
            return NotFound(new { message = "Patient not found." });

        // Patients can only query their own data
        if (userRole == "Patient" && patientId != userId)
            return Forbid();

        // Get or create conversation
        var conversationId = request.ConversationId;
        if (!conversationId.HasValue)
        {
            var conversation = await _ragQueryService.GetOrCreateConversationAsync(
                patientId,
                userId.Value,
                tenantId.Value,
                ct: HttpContext.RequestAborted);
            conversationId = conversation.Id;
        }

        try
        {
            var response = await _ragQueryService.QueryPatientContextAsync(
                patientId,
                request.Message,
                tenantId.Value,
                conversationId,
                HttpContext.RequestAborted);

            _logger.LogInformation(
                "RAG query for Patient {PatientId} by User {UserId}: {Query}",
                patientId, userId.Value, request.Message[..Math.Min(100, request.Message.Length)]);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RAG query");
            return StatusCode(500, new { message = "Error processing your request. Please try again." });
        }
    }

    /// <summary>
    /// Get AI-generated patient health summary for doctors
    /// </summary>
    [HttpGet("patient/{patientId}/summary")]
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    public async Task<ActionResult<PatientHealthSummary>> GetPatientSummary(
        int patientId,
        [FromQuery] SummaryDepth depth = SummaryDepth.Standard)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null)
            return Unauthorized();

        // Validate patient belongs to tenant
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId);
        if (patient == null)
            return NotFound(new { message = "Patient not found." });

        try
        {
            var summary = await _ragQueryService.GetPatientSummaryAsync(
                patientId,
                tenantId.Value,
                depth,
                HttpContext.RequestAborted);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating patient summary");
            return StatusCode(500, new { message = "Error generating summary. Please try again." });
        }
    }

    /// <summary>
    /// Get or create a conversation
    /// </summary>
    [HttpGet("patient/{patientId}/conversation")]
    [Authorize(Roles = "Patient,Doctor,Nurse,Admin")]
    public async Task<ActionResult<AIConversationDto>> GetOrCreateConversation(
        int patientId,
        [FromQuery] string? title = null)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (tenantId == null || userId == null)
            return Unauthorized();

        // Validate patient belongs to tenant
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId);
        if (patient == null)
            return NotFound(new { message = "Patient not found." });

        // Patients can only access their own conversations
        if (userRole == "Patient" && patientId != userId)
            return Forbid();

        var conversation = await _ragQueryService.GetOrCreateConversationAsync(
            patientId,
            userId.Value,
            tenantId.Value,
            title,
            HttpContext.RequestAborted);

        return Ok(conversation);
    }

    /// <summary>
    /// Get conversation messages
    /// </summary>
    [HttpGet("conversation/{conversationId}/messages")]
    [Authorize(Roles = "Patient,Doctor,Nurse,Admin")]
    public async Task<ActionResult<List<AIMessageDto>>> GetConversationMessages(int conversationId)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (tenantId == null || userId == null)
            return Unauthorized();

        // Verify conversation belongs to tenant and user
        var conversation = await _context.AIConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);
        if (conversation == null)
            return NotFound(new { message = "Conversation not found." });

        // Patients can only access their own conversations
        if (userRole == "Patient" && conversation.PatientId != userId)
            return Forbid();

        var messages = await _ragQueryService.GetConversationMessagesAsync(
            conversationId,
            tenantId.Value,
            HttpContext.RequestAborted);

        return Ok(messages);
    }

    /// <summary>
    /// List conversations for a patient
    /// </summary>
    [HttpGet("patient/{patientId}/conversations")]
    [Authorize(Roles = "Patient,Doctor,Nurse,Admin")]
    public async Task<ActionResult<ConversationListResponse>> GetConversations(
        int patientId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = GetCurrentTenantId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (tenantId == null || userId == null)
            return Unauthorized();

        // Validate patient belongs to tenant
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId);
        if (patient == null)
            return NotFound(new { message = "Patient not found." });

        // Patients can only see their own conversations
        if (userRole == "Patient" && patientId != userId)
            return Forbid();

        var query = _context.AIConversations
            .Where(c => c.TenantId == tenantId && c.PatientId == patientId && c.IsActive)
            .OrderByDescending(c => c.UpdatedAt);

        var conversations = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(HttpContext.RequestAborted);

        var totalCount = await query.CountAsync(HttpContext.RequestAborted);

        var result = new List<AIConversationDto>();
        foreach (var conv in conversations)
        {
            result.Add(new AIConversationDto
            {
                Id = conv.Id,
                PatientId = conv.PatientId,
                Title = conv.Title,
                CreatedAt = conv.CreatedAt,
                UpdatedAt = conv.UpdatedAt,
                MessageCount = await _context.AIMessages.CountAsync(m => m.ConversationId == conv.Id, HttpContext.RequestAborted)
            });
        }

        return Ok(new ConversationListResponse
        {
            Conversations = result,
            TotalCount = totalCount
        });
    }

    private int? GetCurrentTenantId()
    {
        var claim = User.FindFirst("TenantId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("nameidentifier")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
    }
}
