using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using CompiaBackend.DTOs;

namespace CompiaBackend.Services;

public class EmailService(IConfiguration config)
{
    // ── Confirmação de cadastro ───────────────────────────────────
    public async Task SendConfirmationEmailAsync(string toEmail, string toName, string token)
    {
        var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:8080";
        var confirmUrl  = $"{frontendUrl}/confirmar-email?token={token}";

        var body = $"""
            <div style="font-family:sans-serif;max-width:500px;margin:auto;padding:24px">
              <h2 style="color:#F5A623;margin:0 0 8px">Confirme seu e-mail</h2>
              <p>Olá, <strong>{toName}</strong>!</p>
              <p>Clique no botão abaixo para confirmar seu cadastro na COMPIA Editora:</p>
              <a href="{confirmUrl}"
                 style="display:inline-block;background:#F5A623;color:#fff;
                        padding:12px 28px;border-radius:8px;text-decoration:none;
                        font-weight:bold;margin:16px 0;font-size:15px">
                Confirmar E-mail
              </a>
              <p style="color:#888;font-size:12px;margin-top:24px">
                Se você não criou uma conta, ignore este e-mail.<br/>
                O link expira em 24 horas.
              </p>
            </div>
            """;

        await SendAsync(toEmail, toName, "Confirme seu e-mail — COMPIA Editora", body);
    }

    // ── Confirmação de pedido ─────────────────────────────────────
    public async Task SendOrderConfirmationAsync(
        string toEmail,
        string toName,
        string orderNumber,
        List<OrderItemRequest> items,
        decimal subtotal,
        decimal shippingPrice,
        decimal total,
        string shippingMethod,
        string paymentMethod)
    {
        bool hasPhysical = items.Any(i => i.ProductType == "livro_fisico");
        bool allDigital  = !hasPhysical;

        // ── Tabela de itens ───────────────────────────────────────
        var itemRows = string.Join("\n", items.Select(i =>
        {
            var typeLabel = i.ProductType switch
            {
                "ebook" => "📱 E-book",
                "kit"   => "📦 Kit",
                _       => "📚 Livro Físico"
            };
            var lineTotal = (i.UnitPrice * i.Quantity).ToString("C", new System.Globalization.CultureInfo("pt-BR"));
            var unitPrice = i.UnitPrice.ToString("C", new System.Globalization.CultureInfo("pt-BR"));

            return $"""
                <tr>
                  <td style="padding:10px 12px;border-bottom:1px solid #f0f0f0">
                    <strong>{i.ProductTitle}</strong><br/>
                    <span style="font-size:12px;color:#888">{typeLabel} · Qtd: {i.Quantity} · {unitPrice} un.</span>
                  </td>
                  <td style="padding:10px 12px;border-bottom:1px solid #f0f0f0;text-align:right;font-weight:600">{lineTotal}</td>
                </tr>
                """;
        }));

        // ── Linha de frete ────────────────────────────────────────
        string shippingRow;
        if (allDigital)
        {
            shippingRow = """
                <tr>
                  <td style="padding:10px 12px;color:#16a34a">🎉 Entrega digital imediata</td>
                  <td style="padding:10px 12px;text-align:right;color:#16a34a;font-weight:600">Grátis</td>
                </tr>
                """;
        }
        else
        {
            var shippingLabel = shippingMethod switch
            {
                "correios_pac"   => "Correios PAC",
                "correios_sedex" => "Correios SEDEX",
                "retirada"       => "Retirada no Local",
                _                => shippingMethod
            };
            var shippingFormatted = shippingPrice == 0
                ? "Grátis"
                : shippingPrice.ToString("C", new System.Globalization.CultureInfo("pt-BR"));

            shippingRow = $"""
                <tr>
                  <td style="padding:10px 12px;color:#555">🚚 Frete ({shippingLabel})</td>
                  <td style="padding:10px 12px;text-align:right">{shippingFormatted}</td>
                </tr>
                """;
        }

        // ── Pagamento ─────────────────────────────────────────────
        var paymentLabel = paymentMethod switch
        {
            "pix"  => "⚡ PIX",
            "card" => "💳 Cartão de Crédito",
            _      => paymentMethod
        };

        // ── Bloco de entrega digital ──────────────────────────────
        var digitalBlock = allDigital
            ? """
              <div style="background:#f0fdf4;border:1px solid #bbf7d0;border-radius:8px;padding:16px;margin:20px 0">
                <strong style="color:#15803d">🎉 Acesso imediato!</strong>
                <p style="margin:6px 0 0;color:#166534;font-size:14px">
                  Seus e-books e kits já estão disponíveis na sua área de cliente.
                  Acesse em <a href="{frontendUrl}/cliente" style="color:#15803d">Minha Conta</a>.
                </p>
              </div>
              """
            : """
              <div style="background:#fffbeb;border:1px solid #fde68a;border-radius:8px;padding:16px;margin:20px 0">
                <strong style="color:#92400e">📦 Acompanhe seu pedido</strong>
                <p style="margin:6px 0 0;color:#78350f;font-size:14px">
                  Você receberá um e-mail com o código de rastreamento assim que o pedido for despachado.
                </p>
              </div>
              """;

        var totalFormatted    = total.ToString("C",    new System.Globalization.CultureInfo("pt-BR"));
        var subtotalFormatted = subtotal.ToString("C", new System.Globalization.CultureInfo("pt-BR"));

        var body = $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;padding:24px;color:#1a1a1a">

              <!-- Header -->
              <div style="text-align:center;margin-bottom:32px">
                <h1 style="color:#F5A623;margin:0;font-size:26px">COMPIA Editora</h1>
                <p style="color:#555;margin:4px 0 0">Confirmação de Pedido</p>
              </div>

              <p>Olá, <strong>{toName}</strong>! 👋</p>
              <p>Seu pedido foi confirmado com sucesso. Aqui estão os detalhes:</p>

              <!-- Número do pedido -->
              <div style="background:#fafafa;border:1px solid #e5e5e5;border-radius:8px;padding:14px 18px;margin:16px 0">
                <span style="color:#888;font-size:13px">Número do pedido</span><br/>
                <strong style="font-size:20px;color:#F5A623">{orderNumber}</strong>
                <span style="float:right;background:#dcfce7;color:#15803d;padding:4px 10px;
                             border-radius:999px;font-size:12px;font-weight:600;margin-top:4px">
                  Confirmado ✓
                </span>
              </div>

              {digitalBlock}

              <!-- Itens -->
              <table style="width:100%;border-collapse:collapse;margin-top:20px">
                <thead>
                  <tr style="background:#f9f9f9">
                    <th style="padding:10px 12px;text-align:left;font-size:13px;color:#555">Produto</th>
                    <th style="padding:10px 12px;text-align:right;font-size:13px;color:#555">Valor</th>
                  </tr>
                </thead>
                <tbody>
                  {itemRows}
                  {shippingRow}
                  <tr style="background:#fafafa">
                    <td style="padding:12px;font-weight:700;font-size:16px">Total</td>
                    <td style="padding:12px;text-align:right;font-weight:700;font-size:16px;color:#F5A623">{totalFormatted}</td>
                  </tr>
                </tbody>
              </table>

              <!-- Pagamento -->
              <p style="margin-top:20px;color:#555">
                <strong>Forma de pagamento:</strong> {paymentLabel}
              </p>

              <!-- Footer -->
              <hr style="border:none;border-top:1px solid #eee;margin:28px 0"/>
              <p style="font-size:12px;color:#aaa;text-align:center">
                COMPIA Editora · compiaeditorabookstore@gmail.com<br/>
                Este é um e-mail automático, não é necessário responder.
              </p>

            </div>
            """;

        await SendAsync(toEmail, toName, $"Pedido {orderNumber} confirmado — COMPIA Editora", body);
    }

    // ── Envio genérico ────────────────────────────────────────────
    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            config["Email:SenderName"]  ?? "COMPIA Editora",
            config["Email:SenderEmail"] ?? "noreply@compia.com"
        ));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            config["Email:SmtpHost"],
            int.Parse(config["Email:SmtpPort"] ?? "587"),
            SecureSocketOptions.StartTls
        );
        await client.AuthenticateAsync(config["Email:Username"], config["Email:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}