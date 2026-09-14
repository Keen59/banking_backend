using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Application.Interfaces.Repositories;

public interface IEmailOtpRepository : IRepository<EmailOtp>
{
    Task<EmailOtp?> GetActiveAsync(Guid userId, EOtpPurpose purpose, CancellationToken cancellationToken = default);

    Task<EmailOtp?> GetLatestAsync(Guid userId, EOtpPurpose purpose, CancellationToken cancellationToken = default);

    Task<List<EmailOtp>> GetUnconsumedAsync(Guid userId, EOtpPurpose purpose, CancellationToken cancellationToken = default);
}
