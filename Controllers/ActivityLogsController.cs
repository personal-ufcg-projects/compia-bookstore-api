using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompiaBackend.Data;

namespace CompiaBackend.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize(Roles = "admin")]
public class ActivityLogsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? userId = null)
    {
        var query = db.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var userGuid))
            query = query.Where(l => l.UserId == userGuid);

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.UserId,
                l.Action,
                l.Details,
                l.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, logs });
    }
}