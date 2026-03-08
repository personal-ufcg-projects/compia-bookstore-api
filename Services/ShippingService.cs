using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CompiaBackend.DTOs;

namespace CompiaBackend.Services;

/// <summary>
/// Calcula frete via Melhor Envio (sandbox ou produção).
///
/// Configurar via user-secrets:
///   dotnet user-secrets set "MelhorEnvio:Token"    "SEU_TOKEN"
///   dotnet user-secrets set "MelhorEnvio:Sandbox"  "true"   ← remover em produção
/// </summary>
public class ShippingService(
    IConfiguration config,
    IHttpClientFactory httpFactory,
    ILogger<ShippingService> logger)
{
    // CEP de origem: R. Aprígio Veloso, 882 — Campina Grande/PB
    private const string CepOrigem = "58429900";

    // IDs dos Correios no Melhor Envio: 1=PAC, 2=SEDEX
    private static readonly int[] ServiceIds = [1, 2];

    public async Task<ShippingQuoteResponse> GetQuoteAsync(ShippingQuoteRequest req)
    {
        // Pedidos sem itens físicos não têm frete
        bool hasPhysical = req.Items.Any(i => i.ProductType == "livro_fisico");
        if (!hasPhysical)
            return new ShippingQuoteResponse([]);

        var token = config["MelhorEnvio:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("MelhorEnvio:Token não configurado. Usando estimativa regional.");
            return GetRegionalEstimate(req.Cep);
        }

        try
        {
            return await GetMelhorEnvioQuoteAsync(req, token);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Melhor Envio indisponível, usando estimativa regional. Erro: {Msg}", ex.Message);
            return GetRegionalEstimate(req.Cep);
        }
    }

    // ── Integração com Melhor Envio ───────────────────────────────
    private async Task<ShippingQuoteResponse> GetMelhorEnvioQuoteAsync(
        ShippingQuoteRequest req, string token)
    {
        bool isSandbox = config["MelhorEnvio:Sandbox"] != "false";
        var baseUrl = isSandbox
            ? "https://sandbox.melhorenvio.com.br"
            : "https://melhorenvio.com.br";

        var client = httpFactory.CreateClient("melhorenvio");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "COMPIA-Editora/1.0 (compiaeditorabookstore@gmail.com)");

        // Peso total: ~400g por livro físico, mínimo 300g
        int physQty = req.Items
            .Where(i => i.ProductType == "livro_fisico")
            .Sum(i => i.Quantity);
        double peso = Math.Max(physQty * 0.4, 0.3);

        var body = new
        {
            from = new { postal_code = CepOrigem },
            to   = new { postal_code = req.Cep.Replace("-", "") },
            package = new
            {
                height = 15,
                width  = 20,
                length = 30,
                weight = peso,
            },
            services = string.Join(",", ServiceIds),
            options  = new { receipt = false, own_hand = false },
        };

        var json    = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{baseUrl}/api/v2/me/shipment/calculate", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var results      = JsonDocument.Parse(responseJson).RootElement;

        var options = new List<ShippingOption>();

        foreach (var svc in results.EnumerateArray())
        {
            // Pula serviços com erro
            if (svc.TryGetProperty("error", out _)) continue;

            var name  = svc.TryGetProperty("name",          out var n) ? n.GetString() ?? "" : "";
            var price = 0m;
            if (svc.TryGetProperty("custom_price", out var p))
            {
                if (p.ValueKind == JsonValueKind.Number) price = p.GetDecimal();
                else if (p.ValueKind == JsonValueKind.String)
                    decimal.TryParse(p.GetString(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out price);
            }
            var days  = svc.TryGetProperty("delivery_time", out var d) ? d.GetInt32()   : 0;
            var id    = svc.TryGetProperty("id",            out var i) ? i.GetInt32()   : 0;

            if (price == 0) continue;

            var shippingId = id switch
            {
                1 => "correios_pac",
                2 => "correios_sedex",
                _ => name.ToLower().Replace(" ", "_"),
            };

            var prazo = days == 1 ? "1 dia útil" : $"{days} dias úteis";

            options.Add(new ShippingOption(
                Id:            shippingId,
                Label:         name,
                Description:   id == 1 ? "Entrega econômica" : "Entrega expressa",
                Price:         price,
                EstimatedDays: prazo
            ));
        }

        // Retirada sempre disponível
        options.Add(new ShippingOption(
            Id:            "retirada",
            Label:         "Retirada no Local",
            Description:   "Retire em nossa loja — Campina Grande/PB",
            Price:         0m,
            EstimatedDays: "Disponível em 1 dia útil"
        ));

        return new ShippingQuoteResponse(options);
    }

    // ── Fallback regional (quando Melhor Envio indisponível) ──────
    private static ShippingQuoteResponse GetRegionalEstimate(string cep)
    {
        var digits = cep.Replace("-", "");
        if (digits.Length < 2 || !int.TryParse(digits[..2], out var prefix))
            prefix = 99;

        var (pac, sedex, pacDias, sedexDias) = prefix switch
        {
            >= 1  and <= 19 => (14.90m, 24.90m, "5-8",   "2-3"),  // SP
            >= 20 and <= 39 => (18.90m, 30.90m, "5-8",   "2-3"),  // RJ/MG/ES
            >= 80 and <= 99 => (22.90m, 36.90m, "7-10",  "3-4"),  // Sul
            >= 40 and <= 65 => (12.90m, 22.90m, "3-6",   "1-2"),  // Nordeste (perto de CG)
            _               => (26.90m, 44.90m, "10-14", "4-6"),  // CO/Norte
        };

        return new ShippingQuoteResponse([
            new("correios_pac",   "Correios PAC",      "Entrega econômica",    pac,   $"{pacDias} dias úteis"),
            new("correios_sedex", "Correios SEDEX",    "Entrega expressa",     sedex, $"{sedexDias} dias úteis"),
            new("retirada",       "Retirada no Local", "Retire em Campina Grande/PB", 0m, "Disponível em 1 dia útil"),
        ]);
    }
}