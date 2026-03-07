using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CompiaBackend.Services;

public class EmailService(IConfiguration config)
{
    public async Task SendConfirmationEmailAsync(string toEmail, string toName, string token)
    {
        var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:5173";
        var confirmUrl  = $"{frontendUrl}/confirmar-email?token={token}";

        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:auto">
              <h2 style="color:#F5A623">Confirme seu e-mail</h2>
              <p>Olá, <strong>{toName}</strong>!</p>
              <p>Clique no botão abaixo para confirmar seu cadastro na COMPIA Editora:</p>
              <a href="{confirmUrl}"
                 style="display:inline-block;background:#F5A623;color:#fff;
                        padding:12px 24px;border-radius:8px;text-decoration:none;
                        font-weight:bold;margin:16px 0">
                Confirmar E-mail
              </a>
              <p style="color:#888;font-size:12px">
                Se você não criou uma conta, ignore este e-mail.<br/>
                O link expira em 24 horas.
              </p>
            </div>
            """;

        await SendAsync(toEmail, toName, "Confirme seu e-mail — COMPIA Editora", body);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            config["Email:SenderName"] ?? "COMPIA Editora",
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

        await client.AuthenticateAsync(
            config["Email:Username"],
            config["Email:Password"]
        );

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}