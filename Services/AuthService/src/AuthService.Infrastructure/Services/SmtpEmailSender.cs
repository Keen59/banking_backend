using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AuthService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace AuthService.Infrastructure.Services;

public class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var email = configuration.GetSection("Email");
        var host = email["Host"] ?? throw new InvalidOperationException("Email:Host yapılandırılmamış.");
        var from = email["From"] ?? throw new InvalidOperationException("Email:From yapılandırılmamış.");
        var port = int.TryParse(email["Port"], out var parsedPort) ? parsedPort : 587;
        var enableSsl = !bool.TryParse(email["EnableSsl"], out var ssl) || ssl;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            host,
            port,
            enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);

        var username = email["Username"];
        var password = email["Password"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            await client.AuthenticateAsync(username, password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
