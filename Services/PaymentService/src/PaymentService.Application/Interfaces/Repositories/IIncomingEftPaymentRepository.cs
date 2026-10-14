using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories;

public interface IIncomingEftPaymentRepository : IRepository<IncomingEftPayment>
{
    Task<IncomingEftPayment?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncomingEftPayment>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
