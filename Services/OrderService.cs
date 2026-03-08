using Microsoft.EntityFrameworkCore;
using CompiaBackend.Data;
using CompiaBackend.DTOs;
using CompiaBackend.Models;

namespace CompiaBackend.Services;

public class OrderService(AppDbContext db, EmailService emailService, ILogger<OrderService> logger)
{
    public async Task<CreateOrderResponse> CreateAsync(CreateOrderRequest req, Guid userId)
    {
        bool hasPhysical = req.Items.Any(i => i.ProductType == "livro_fisico");
        bool allDigital  = !hasPhysical;

        // ── Gera número do pedido ─────────────────────────────────
        var orderNumber = $"CMP-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";

        // ── Calcula frete ─────────────────────────────────────────
        decimal shippingPrice = 0m;
        if (hasPhysical)
        {
            shippingPrice = req.ShippingMethodId switch
            {
                "correios_pac"   => req.ShippingPrice ?? 14.90m,
                "correios_sedex" => req.ShippingPrice ?? 29.90m,
                "retirada"       => 0m,
                _                => 0m
            };
        }

        var subtotal = req.Items.Sum(i => i.UnitPrice * i.Quantity);
        var total    = subtotal + shippingPrice;

        // ── Cria o pedido ─────────────────────────────────────────
        var order = new Order
        {
            OrderNumber    = orderNumber,
            UserId         = userId,
            Nome           = req.Address.Nome,
            Email          = req.Address.Email,
            Cep            = req.Address.Cep,
            Endereco       = req.Address.Endereco,
            Numero         = req.Address.Numero,
            Complemento    = req.Address.Complemento,
            Bairro         = req.Address.Bairro,
            Cidade         = req.Address.Cidade,
            Estado         = req.Address.Estado,
            ShippingMethod = allDigital ? "digital" : req.ShippingMethodId,
            ShippingPrice  = shippingPrice,
            PaymentMethod  = req.PaymentMethod,
            Total          = total,
            // Ebooks e kits vão direto para "Disponível"; físicos ficam em "Processando"
            Status = allDigital ? "Disponível" : "Processando",
            Items = req.Items.Select(i => new OrderItem
            {
                ProductId    = i.ProductId,
                ProductTitle = i.ProductTitle,
                ProductType  = i.ProductType,
                Quantity     = i.Quantity,
                UnitPrice    = i.UnitPrice,
            }).ToList()
        };

        db.Orders.Add(order);

        db.ActivityLogs.Add(new ActivityLog
        {
            UserId     = userId,
            Action     = "order_created",
            EntityType = "order",
            EntityId   = order.Id.ToString(),
            Details    = $"{{\"orderNumber\":\"{orderNumber}\",\"total\":{total},\"items\":{req.Items.Count},\"allDigital\":{allDigital.ToString().ToLower()}}}"
        });

        await db.SaveChangesAsync();

        // ── Envia e-mail de confirmação ───────────────────────────
        try
        {
            await emailService.SendOrderConfirmationAsync(
                toEmail:        req.Address.Email,
                toName:         req.Address.Nome,
                orderNumber:    orderNumber,
                items:          req.Items,
                subtotal:       subtotal,
                shippingPrice:  shippingPrice,
                total:          total,
                shippingMethod: order.ShippingMethod,
                paymentMethod:  req.PaymentMethod
            );
        }
        catch (Exception ex)
        {
            // Email não deve impedir a criação do pedido
            logger.LogError(ex, "Falha ao enviar e-mail de confirmação do pedido {OrderNumber}", orderNumber);
        }

        // ── PIX mock ──────────────────────────────────────────────
        string? pixCode = req.PaymentMethod == "pix"
            ? $"00020126360014br.gov.bcb.pix{Guid.NewGuid():N}"
            : null;

        return new CreateOrderResponse(orderNumber, "confirmed", pixCode);
    }

    public async Task<List<OrderSummary>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(ToSummary).ToList();
    }

    public async Task<List<OrderSummary>> GetAllOrdersAsync()
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(ToSummary).ToList();
    }

    private static OrderSummary ToSummary(Order o) => new(
        o.Id.ToString(),
        o.OrderNumber,
        o.Status,
        o.Total,
        o.CreatedAt,
        o.Items.Count,
        o.Items.Select(i => new OrderSummaryItem(
            i.ProductId,
            i.ProductTitle,
            i.ProductType,
            i.Quantity
        )).ToList()
    );

    public async Task<bool> UpdateStatusAsync(Guid orderId, string newStatus)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order is null) return false;
        order.Status = newStatus;
        await db.SaveChangesAsync();
        return true;
    }
}