using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories;

public interface IFastPaymentRepository : IRepository<FastPayment>
{
    Task<FastPayment?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastPayment>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumCountedTowardDailyLimitAsync(
        Guid customerId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default);
}
