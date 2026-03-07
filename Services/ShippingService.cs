using CompiaBackend.DTOs;

namespace CompiaBackend.Services;

public class ShippingService
{
    // Quando tiver integração real com Correios ou transportadora,
    // substitua a lógica aqui — o controller não precisa mudar.
    public Task<ShippingQuoteResponse> GetQuoteAsync(ShippingQuoteRequest req)
    {
        var options = new List<ShippingOption>
        {
            new("correios_pac",    "Correios PAC",       "Entrega econômica",                   14.90m, "8-12 dias úteis"),
            new("correios_sedex",  "Correios SEDEX",     "Entrega expressa",                    29.90m, "3-5 dias úteis"),
            new("transportadora",  "Transportadora",     "Entrega com rastreamento completo",   22.50m, "5-7 dias úteis"),
            new("retirada",        "Retirada no Local",  "Retire em nossa loja — Av. Paulista", 0m,     "Disponível em 1 dia útil"),
        };

        return Task.FromResult(new ShippingQuoteResponse(options));
    }
}