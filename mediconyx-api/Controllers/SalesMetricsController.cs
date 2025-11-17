using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using mediconyx_api.Data;
using mediconyx_api.Models;

namespace mediconyx_api.Controllers;

[ApiController]
[Route("api/sales/metrics")]
[Authorize(Roles = "Sales,Master")]
public class SalesMetricsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SalesMetricsController> _logger;

    public SalesMetricsController(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<SalesMetricsController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<object>> Kpis()
    {
        const string cacheKey = "sales_kpis";
        
        if (_cache.TryGetValue(cacheKey, out object? cachedKpis))
        {
            _logger.LogDebug("Returning cached KPIs");
            return Ok(cachedKpis);
        }

        var totalOnboarded = await _context.SalesRequests.CountAsync(r => r.Status == SalesRequestStatus.Active);
        var inProgress = await _context.SalesRequests.CountAsync(r => r.Status == SalesRequestStatus.InProgress);
        var active = await _context.SalesRequests.CountAsync(r => r.Status == SalesRequestStatus.Active);
        var pending = await _context.SalesRequests.CountAsync(r => r.Status == SalesRequestStatus.Pending);
        var deactivated = await _context.SalesRequests.CountAsync(r => r.Status == SalesRequestStatus.Deactivated);
        
        var result = new { totalOnboarded, inProgress, active, pending, deactivated };
        
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        _logger.LogDebug("KPIs calculated and cached for 5 minutes");
        
        return Ok(result);
    }

    [HttpGet("trends")]
    public async Task<ActionResult<object>> Trends([FromQuery] string bucket = "month", [FromQuery] int months = 6)
    {
        var cacheKey = $"sales_trends_{bucket}_{months}";
        
        if (_cache.TryGetValue(cacheKey, out object? cachedTrends))
        {
            _logger.LogDebug("Returning cached trends for {Bucket}, {Months} months", bucket, months);
            return Ok(cachedTrends);
        }

        var from = DateTime.UtcNow.AddMonths(-months);
        var data = await _context.SalesRequests
            .Where(r => r.CreatedAt >= from)
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .Select(g => new { period = $"{g.Key.Year}-{g.Key.Month:D2}", count = g.Count() })
            .OrderBy(x => x.period)
            .ToListAsync();
        var result = data.Select((x, i) => new
        {
            x.period,
            x.count,
            deltaPercent = i == 0 ? 0 : (double)(x.count - data[i - 1].count) / Math.Max(1, data[i - 1].count) * 100.0
        });
        
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        _logger.LogDebug("Trends calculated and cached for 10 minutes");
        
        return Ok(result);
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<object>> Revenue([FromQuery] string bucket = "month", [FromQuery] int months = 6)
    {
        var cacheKey = $"sales_revenue_{bucket}_{months}";
        
        if (_cache.TryGetValue(cacheKey, out object? cachedRevenue))
        {
            _logger.LogDebug("Returning cached revenue for {Bucket}, {Months} months", bucket, months);
            return Ok(cachedRevenue);
        }

        var from = DateTime.UtcNow.AddMonths(-months);
        var quotedRaw = await _context.SalesRequests
            .Where(r => r.CreatedAt >= from && r.QuotedPrice != null)
            .GroupBy(r => new { Year = r.CreatedAt.Year, Month = r.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, quotedRevenue = g.Sum(x => x.QuotedPrice ?? 0) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var realizedRaw = await _context.SalesRequests
            .Where(r => r.PaidAt != null)
            .GroupBy(r => new { Year = r.PaidAt!.Value.Year, Month = r.PaidAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, realizedRevenue = g.Sum(x => x.QuotedPrice ?? 0) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var periods = quotedRaw.Select(x => (x.Year, x.Month))
            .Union(realizedRaw.Select(x => (x.Year, x.Month)))
            .Distinct()
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var activeCount = await _context.SalesRequests.CountAsync(r => r.Status == SalesRequestStatus.Active);
        var totalRealized = realizedRaw.Sum(x => x.realizedRevenue);
        var arpu = activeCount > 0 ? totalRealized / activeCount : 0;

        var merged = periods.Select(p => new
        {
            period = $"{p.Year}-{p.Month:D2}",
            quotedRevenue = quotedRaw.FirstOrDefault(x => x.Year == p.Year && x.Month == p.Month)?.quotedRevenue ?? 0,
            realizedRevenue = realizedRaw.FirstOrDefault(x => x.Year == p.Year && x.Month == p.Month)?.realizedRevenue ?? 0,
            arpu
        });

        _cache.Set(cacheKey, merged, TimeSpan.FromMinutes(10));
        _logger.LogDebug("Revenue calculated and cached for 10 minutes");

        return Ok(merged);
    }
}
