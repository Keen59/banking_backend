using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Repositories;

public class IncomingFastPaymentRepository(DBContext context)
    : Repository<IncomingFastPayment>(context), IIncomingFastPaymentRepository
{
    public async Task<IncomingFastPayment?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.IncomingFastPayment.FirstOrDefaultAsync(
            x => x.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<IncomingFastPayment>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.IncomingFastPayment
            .AsNoTracking()
            .Where(x => x.AccountCustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
