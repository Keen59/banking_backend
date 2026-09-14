using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class EmailOtpRepository : Repository<EmailOtp>, IEmailOtpRepository
{
    private readonly DBContext context;

    public EmailOtpRepository(DBContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<EmailOtp?> GetActiveAsync(
        Guid userId,
        EOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await context.EmailOtp
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EmailOtp?> GetLatestAsync(
        Guid userId,
        EOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await context.EmailOtp
            .Where(x => x.UserId == userId && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<EmailOtp>> GetUnconsumedAsync(
        Guid userId,
        EOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await context.EmailOtp
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.ConsumedAt == null)
            .ToListAsync(cancellationToken);
    }
}
