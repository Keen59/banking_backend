using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories;

public interface IIncomingFastPaymentRepository : IRepository<IncomingFastPayment>
{
    Task<IncomingFastPayment?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncomingFastPayment>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
