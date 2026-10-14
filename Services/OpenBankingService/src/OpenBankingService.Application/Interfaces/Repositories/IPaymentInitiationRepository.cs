using OpenBankingService.Domain.Entities;

namespace OpenBankingService.Application.Interfaces.Repositories;

public interface IPaymentInitiationRepository : IRepository<PaymentInitiation>
{
    Task<PaymentInitiation?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
