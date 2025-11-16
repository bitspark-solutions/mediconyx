using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Services;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/admin/sales-users")]
[Authorize(Roles = "Master")]
public class SalesUsersAdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _password;

    public SalesUsersAdminController(ApplicationDbContext context, IPasswordService password)
    {
        _context = context;
        _password = password;
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateSalesUser request)
    {
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return BadRequest(new { message = "Email already exists." });
        }
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Name == "Platform");
        if (tenant == null) return BadRequest(new { message = "Platform tenant missing." });
        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _password.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Sales,
            TenantId = tenant.Id,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Created($"/api/users/{user.Id}", new { id = user.Id });
    }

    [HttpGet]
    public async Task<ActionResult<object>> List()
    {
        var users = await _context.Users.Where(u => u.Role == UserRole.Sales).Select(u => new
        {
            u.Id,
            u.Email,
            u.FirstName,
            u.LastName,
            u.IsActive,
            u.LastLoginAt
        }).ToListAsync();
        return Ok(users);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> ToggleActive(int id, [FromBody] ToggleActiveRequest body)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (user.Role != UserRole.Sales) return BadRequest(new { message = "Not a sales user." });
        user.IsActive = body.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateSalesUser
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ToggleActiveRequest
{
    public bool IsActive { get; set; }
}
