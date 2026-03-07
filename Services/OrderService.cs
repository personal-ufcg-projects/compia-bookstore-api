using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CompiaBackend.Data;
using CompiaBackend.DTOs;
using CompiaBackend.Models;

namespace CompiaBackend.Services;

public class OrderService(AppDbContext db)
{
    public async Task<CreateOrderResponse> CreateAsync(CreateOrderRequest req, Guid userId)
    {
        var orderNumber = $"#{Random.Shared.Next(1000, 9999)}";

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Nome = req.Address.Nome,
            Email = req.Address.Email,
            Cep = req.Address.Cep,
            Endereco = req.Address.Endereco,
            Numero = req.Address.Numero,
            Complemento = req.Address.Complemento,
            Bairro = req.Address.Bairro,
            Cidade = req.Address.Cidade,
            Estado = req.Address.Estado,
            ShippingMethod = req.ShippingMethodId,
            PaymentMethod = req.PaymentMethod,
            Status = "Processando",
            Items = req.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
            }).ToList()
        };

        // Calcula preço de frete pelo método escolhido
        order.ShippingPrice = req.ShippingMethodId switch
        {
            "correios_pac"   => 14.90m,
            "correios_sedex" => 29.90m,
            "transportadora" => 22.50m,
            "retirada"       => 0m,
            _                => 0m
        };

        order.Total = req.Items.Sum(i => i.UnitPrice * i.Quantity) + order.ShippingPrice;

        db.Orders.Add(order);

        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            Action = "order_created",
            EntityType = "order",
            EntityId = order.Id.ToString(),
            Details = $"{{\"total\":{order.Total},\"items\":{req.Items.Count}}}"
        });

        await db.SaveChangesAsync();

        // Gera código PIX fictício se necessário
        string? pixCode = req.PaymentMethod == "pix"
            ? "00020126360014br.gov.bcb.pix" + Guid.NewGuid().ToString("N")
            : null;

        return new CreateOrderResponse(orderNumber, "confirmed", pixCode);
    }

    public async Task<List<OrderSummary>> GetUserOrdersAsync(Guid userId)
    {
        return await db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummary(
                o.Id.ToString(),
                o.OrderNumber,
                o.Status,
                o.Total,
                o.CreatedAt,
                o.Items.Count
            ))
            .ToListAsync();
    }

    public async Task<List<OrderSummary>> GetAllOrdersAsync()
    {
        return await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummary(
                o.Id.ToString(),
                o.OrderNumber,
                o.Status,
                o.Total,
                o.CreatedAt,
                o.Items.Count
            ))
            .ToListAsync();
    }

    public async Task<bool> UpdateStatusAsync(Guid orderId, string newStatus)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order is null) return false;
        order.Status = newStatus;
        await db.SaveChangesAsync();
        return true;
    }
}