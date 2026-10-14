using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Repositories;

public class IncomingEftPaymentRepository(DBContext context)
    : Repository<IncomingEftPayment>(context), IIncomingEftPaymentRepository
{
    public async Task<IncomingEftPayment?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.IncomingEftPayment.FirstOrDefaultAsync(
            x => x.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<IncomingEftPayment>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.IncomingEftPayment
            .AsNoTracking()
            .Where(x => x.AccountCustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
