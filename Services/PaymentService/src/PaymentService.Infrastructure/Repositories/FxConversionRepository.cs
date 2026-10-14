using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Repositories;

public class FxConversionRepository(DBContext context)
    : Repository<FxConversion>(context), IFxConversionRepository
{
    public async Task<FxConversion?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.FxConversion.FirstOrDefaultAsync(
            x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<FxConversion>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.FxConversion
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> SumCountedTowardDailyLimitAsync(
        Guid customerId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default)
    {
        return await Context.FxConversion
            .Where(x =>
                x.CustomerId == customerId &&
                x.CreatedAt >= fromUtc &&
                x.Status != ETransferStatus.Rejected)
            .SumAsync(x => x.TryEquivalent, cancellationToken);
    }
}
