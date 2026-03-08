using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompiaBackend.Data;

namespace CompiaBackend.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "admin")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var now       = DateTime.UtcNow;
        var thisStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastStart = thisStart.AddMonths(-1);
        var lastEnd   = thisStart;

        // ── Contagens simples ─────────────────────────────────────
        var totalProducts = await db.Products.CountAsync(p => p.IsActive);

        var thisMonthOrders = await db.Orders
            .Where(o => o.CreatedAt >= thisStart)
            .ToListAsync();

        var lastMonthOrders = await db.Orders
            .Where(o => o.CreatedAt >= lastStart && o.CreatedAt < lastEnd)
            .ToListAsync();

        var thisRevenue = thisMonthOrders.Sum(o => o.Total);
        var lastRevenue = lastMonthOrders.Sum(o => o.Total);

        var growth = lastRevenue == 0
            ? (thisRevenue > 0 ? 100 : 0)
            : Math.Round((double)((thisRevenue - lastRevenue) / lastRevenue * 100), 1);

        // ── Últimos 5 pedidos ─────────────────────────────────────
        var recentOrders = await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Total,
                o.CreatedAt,
                CustomerName  = o.Nome,
                CustomerEmail = o.Email,
            })
            .ToListAsync();

        // ── Últimas 5 atividades ──────────────────────────────────
        var recentLogs = await db.ActivityLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new
            {
                l.Id,
                l.UserId,
                l.Action,
                l.Details,
                l.CreatedAt,
            })
            .ToListAsync();

        return Ok(new
        {
            stats = new
            {
                totalProducts,
                monthOrders  = thisMonthOrders.Count,
                monthRevenue = thisRevenue,
                growth,
            },
            recentOrders,
            recentLogs,
        });
    }
}