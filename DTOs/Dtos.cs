namespace CompiaBackend.DTOs;

// ── Auth ──────────────────────────────────────────────
public record RegisterRequest(string FullName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string UserId, string FullName, string Email, string Role);

// ── Shipping ──────────────────────────────────────────
// ProductType: "livro_fisico" | "ebook" | "kit"
public record ShippingItem(string ProductId, int Quantity, string ProductType = "livro_fisico");

public record ShippingQuoteRequest(string Cep, List<ShippingItem> Items);

public record ShippingOption(
    string Id,
    string Label,
    string Description,
    decimal Price,
    string EstimatedDays
);

public record ShippingQuoteResponse(List<ShippingOption> Options);

// ── Orders ────────────────────────────────────────────
public record OrderAddress(
    string Nome, string Email, string Cep,
    string Endereco, string Numero, string Complemento,
    string Bairro, string Cidade, string Estado
);

public record OrderItemRequest(
    string ProductId,
    string ProductTitle,
    int Quantity,
    decimal UnitPrice,
    string ProductType = "livro_fisico"   // "livro_fisico" | "ebook" | "kit"
);

public record CardInfo(string CardNumber, string Expiry, string Cvv, string CardName);

public record CreateOrderRequest(
    OrderAddress Address,
    string ShippingMethodId,     // vazio "" para pedidos só de ebooks/kits
    decimal? ShippingPrice,      // preço calculado pelo frontend via API Correios
    string PaymentMethod,
    CardInfo? Card,
    List<OrderItemRequest> Items
);

public record CreateOrderResponse(string OrderId, string Status, string? PixCode = null);

public record OrderSummaryItem(
    string ProductId,
    string ProductTitle,
    string ProductType,   // "livro_fisico" | "ebook" | "kit"
    int Quantity
);

public record OrderSummary(
    string Id, string OrderNumber, string Status,
    decimal Total, DateTime CreatedAt, int ItemCount,
    List<OrderSummaryItem> Items
);

// ── Admin ─────────────────────────────────────────────
public record LogEntry(
    Guid Id, Guid? UserId, string Action,
    string? EntityType, string? EntityId,
    string? Details, DateTime CreatedAt
);