using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace OpenBankingService.Infrastructure.Repositories;

public class PaymentInitiationRepository(DBContext context)
    : Repository<PaymentInitiation>(context), IPaymentInitiationRepository
{
    public async Task<PaymentInitiation?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.PaymentInitiation.FirstOrDefaultAsync(
            x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }
}
