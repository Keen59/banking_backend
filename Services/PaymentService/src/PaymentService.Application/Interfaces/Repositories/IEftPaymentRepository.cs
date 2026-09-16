using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories;

public interface IEftPaymentRepository : IRepository<EftPayment>
{
    Task<EftPayment?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EftPayment>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumCountedTowardDailyLimitAsync(
        Guid customerId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default);
}
