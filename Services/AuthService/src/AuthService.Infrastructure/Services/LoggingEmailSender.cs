using AuthService.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "E-posta gönderildi. To={To} Subject={Subject} Body={Body}",
            to,
            subject,
            body);

        return Task.CompletedTask;
    }
}
