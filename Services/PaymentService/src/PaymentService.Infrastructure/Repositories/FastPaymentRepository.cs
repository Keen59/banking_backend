using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Repositories;

public class FastPaymentRepository(DBContext context) : Repository<FastPayment>(context), IFastPaymentRepository
{
    public async Task<FastPayment?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.FastPayment.FirstOrDefaultAsync(
            x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<FastPayment>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.FastPayment
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
        return await Context.FastPayment
            .Where(x =>
                x.CustomerId == customerId &&
                x.CreatedAt >= fromUtc &&
                x.Status != ETransferStatus.Rejected)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
    }
}
