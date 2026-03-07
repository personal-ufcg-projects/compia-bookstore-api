using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CompiaBackend.DTOs;
using CompiaBackend.Services;

namespace CompiaBackend.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    /// <summary>Cria um novo pedido (requer login)</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CreateOrderResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await orderService.CreateAsync(req, userId);
        return Ok(result);
    }

    /// <summary>Lista pedidos do usuário logado</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(List<OrderSummary>), 200)]
    public async Task<IActionResult> MyOrders()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await orderService.GetUserOrdersAsync(userId);
        return Ok(orders);
    }

    /// <summary>Lista todos os pedidos (somente admin)</summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(List<OrderSummary>), 200)]
    public async Task<IActionResult> AllOrders()
    {
        var orders = await orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    /// <summary>Atualiza status de um pedido (somente admin)</summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string newStatus)
    {
        var ok = await orderService.UpdateStatusAsync(id, newStatus);
        return ok ? NoContent() : NotFound();
    }
}