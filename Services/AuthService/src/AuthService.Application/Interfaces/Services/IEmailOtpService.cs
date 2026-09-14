using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Application.Interfaces.Services;

public interface IEmailOtpService
{
    Task<bool> IssueAsync(User user, EOtpPurpose purpose, CancellationToken cancellationToken = default);

    Task<User> VerifyAndConsumeAsync(string email, string code, EOtpPurpose purpose, CancellationToken cancellationToken = default);
}
